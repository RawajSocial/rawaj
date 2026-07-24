using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Policies;
using Rawaj.Application.Features.Content.Common;
using Rawaj.Domain.Entities.AiOperations;
using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Content.GenerateVisualAsset;

public class GenerateVisualAssetCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    ICurrentTenantContext currentTenantContext,
    IAiImageGenerationService imageGenerationService,
    ICoinCostProvider coinCostProvider)
    : IRequestHandler<GenerateVisualAssetCommand, Result<GenerateVisualAssetResponse>>
{
    public async Task<Result<GenerateVisualAssetResponse>> Handle(
        GenerateVisualAssetCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;
        var userId = currentUserService.UserId!.Value;
        var role = currentTenantContext.Role!.Value;

        var brand = await dbContext.TenantBrandProfiles
            .FirstOrDefaultAsync(b => b.Id == request.BrandProfileId && b.TenantId == tenantId, cancellationToken);
        if (brand is null)
        {
            return Result<GenerateVisualAssetResponse>.Failure("Brand profile not found.");
        }

        MarketingCampaign? campaign = null;
        if (request.CampaignId.HasValue)
        {
            campaign = await dbContext.MarketingCampaigns
                .FirstOrDefaultAsync(c => c.Id == request.CampaignId.Value && c.BrandProfileId == brand.Id, cancellationToken);
            if (campaign is null)
            {
                return Result<GenerateVisualAssetResponse>.Failure("Campaign not found.");
            }
        }

        if (request.ContentItemId is not null)
        {
            var contentItemExists = await dbContext.ContentItems.AnyAsync(
                c => c.Id == request.ContentItemId && c.BrandProfileId == brand.Id, cancellationToken);
            if (!contentItemExists)
            {
                return Result<GenerateVisualAssetResponse>.Failure("Content item not found.");
            }
        }

        var creditsUsage = await AiCreditsPolicy.GetUsageAsync(dbContext, tenantId, cancellationToken);
        if (!creditsUsage.HasCreditsRemaining)
        {
            return Result<GenerateVisualAssetResponse>.Failure(
                $"Your subscription plan allows {creditsUsage.MaxCreditsMonthly} AI credits per month. Upgrade for more.");
        }

        var tenant = await dbContext.Tenants.FirstAsync(t => t.Id == tenantId, cancellationToken);

        // New-tenant free trial (pricing sheet section 3.2): the first 5 image generations are free.
        var usesFreeTrial = tenant.FreeImageGenerationsRemaining > 0;
        var coinCost = usesFreeTrial
            ? 0
            : await CoinPricingPolicy.GetDiscountedCostAsync(dbContext, tenantId, coinCostProvider.VisualGeneration, cancellationToken);
        var coinBalance = await CoinPolicy.GetBalanceAsync(dbContext, tenantId, userId, role, cancellationToken);
        if (coinBalance < coinCost)
        {
            return Result<GenerateVisualAssetResponse>.Failure(
                $"You need {coinCost} coins to generate an image, but only have {coinBalance}.");
        }

        var prompt = ContentPromptBuilder.BuildImagePrompt(brand, campaign, request.Type.ToString(), request.Prompt);

        var generation = await imageGenerationService.GenerateImageAsync(prompt, cancellationToken);

        var now = DateTime.UtcNow;
        var visualAssetId = Guid.NewGuid();

        var job = new AiJob
        {
            Id = Guid.NewGuid(),
            BrandProfileId = brand.Id,
            TriggeredBy = userId,
            JobType = AiJobType.ImageGeneration,
            Status = generation.Succeeded ? AiJobStatus.Completed : AiJobStatus.Failed,
            InputParams = JsonSerializer.Serialize(new { prompt }),
            OutputRefId = generation.Succeeded ? visualAssetId : null,
            OutputRefType = "visual_asset",
            ErrorMessage = generation.ErrorMessage,
            StartedAt = now,
            CompletedAt = now,
            CreatedAt = now
        };
        dbContext.AiJobs.Add(job);

        if (!generation.Succeeded)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result<GenerateVisualAssetResponse>.Failure(
                generation.ErrorMessage ?? "Image generation failed. Please try again.");
        }

        var dataUrl = $"data:{generation.ContentType};base64,{Convert.ToBase64String(generation.ImageBytes!)}";

        var visualAsset = new VisualAsset
        {
            Id = visualAssetId,
            ContentItemId = request.ContentItemId,
            CampaignId = campaign?.Id,
            BrandProfileId = brand.Id,
            Type = request.Type,
            FileUrl = dataUrl,
            SourceType = VisualAssetSourceType.AiGenerated,
            AiPrompt = prompt,
            Format = generation.ContentType?.Split('/').Last(),
            IsApproved = false,
            CreatedAt = now
        };
        dbContext.VisualAssets.Add(visualAsset);

        if (usesFreeTrial)
        {
            tenant.FreeImageGenerationsRemaining--;
        }
        else
        {
            await CoinPolicy.TrySpendAsync(dbContext, tenantId, userId, role, coinCost, cancellationToken);
        }

        var visualSubject = campaign is not null ? $"for \"{campaign.Name}\"" : $"for {brand.Name}";
        NotificationPublisher.Notify(
            dbContext,
            userId,
            brand.Id,
            NotificationType.Success,
            NotificationCategory.AiJob,
            "Image generated",
            $"A new {request.Type} image {visualSubject} has finished generating.",
            visualAsset.Id,
            "visual_asset");

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<GenerateVisualAssetResponse>.Success(
            new GenerateVisualAssetResponse(visualAsset.Id, campaign?.Id, visualAsset.FileUrl));
    }
}

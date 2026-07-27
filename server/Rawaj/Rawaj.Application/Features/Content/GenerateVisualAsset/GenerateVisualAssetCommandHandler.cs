using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Policies;
using Rawaj.Application.Features.Content.Common;
using Rawaj.Domain.Entities.AiOperations;
using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Content.GenerateVisualAsset;

public class GenerateVisualAssetCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    ICurrentTenantContext currentTenantContext,
    IAiImageGenerationService imageGenerationService,
    IMediaStorageService mediaStorageService,
    ICoinCostProvider coinCostProvider)
    : IRequestHandler<GenerateVisualAssetCommand, Result<GenerateVisualAssetResponse>>
{
    public async Task<Result<GenerateVisualAssetResponse>> Handle(
        GenerateVisualAssetCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;
        var userId = currentUserService.UserId!.Value;
        var role = currentTenantContext.Role!.Value;

        TenantBrandProfile? brand = null;
        if (request.BrandProfileId.HasValue)
        {
            brand = await dbContext.TenantBrandProfiles
                .FirstOrDefaultAsync(b => b.Id == request.BrandProfileId.Value && b.TenantId == tenantId, cancellationToken);
            if (brand is null)
            {
                return Result<GenerateVisualAssetResponse>.Failure("Brand profile not found.");
            }
        }

        if (request.CampaignId.HasValue && brand is null)
        {
            return Result<GenerateVisualAssetResponse>.Failure("A campaign requires a brand profile.");
        }

        MarketingCampaign? campaign = null;
        if (request.CampaignId.HasValue)
        {
            campaign = await dbContext.MarketingCampaigns
                .FirstOrDefaultAsync(c => c.Id == request.CampaignId.Value && c.BrandProfileId == brand!.Id, cancellationToken);
            if (campaign is null)
            {
                return Result<GenerateVisualAssetResponse>.Failure("Campaign not found.");
            }
        }

        // When this is a retry for an existing post, the post's own stored English scene
        // description is a far better image prompt than what the caller sends — the content-review
        // page can only send the post's Arabic marketing copy, which the image model renders
        // poorly (see ContentItem.ImagePrompt). Null for pre-existing/standalone items, in which
        // case the caller's prompt is used as before.
        string? storedImagePrompt = null;
        if (request.ContentItemId is not null)
        {
            var brandIdForLookup = brand?.Id;
            var contentItem = await dbContext.ContentItems
                .Where(c => c.Id == request.ContentItemId && c.BrandProfileId == brandIdForLookup)
                .Select(c => new { c.ImagePrompt })
                .FirstOrDefaultAsync(cancellationToken);
            if (contentItem is null)
            {
                return Result<GenerateVisualAssetResponse>.Failure("Content item not found.");
            }

            storedImagePrompt = string.IsNullOrWhiteSpace(contentItem.ImagePrompt) ? null : contentItem.ImagePrompt;
        }

        var generationMode = campaign is not null ? GenerationMode.Campaign : brand is not null ? GenerationMode.Brand : GenerationMode.Standalone;

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
                CoinPolicy.InsufficientCoinsMessage(coinCost, coinBalance, "generate an image"));
        }

        var subject = storedImagePrompt ?? request.Prompt;
        var prompt = brand is not null
            ? ContentPromptBuilder.BuildImagePrompt(brand, campaign, request.Type.ToString(), subject)
            : ContentPromptBuilder.BuildStandaloneImagePrompt(request.Type.ToString(), subject);

        var startedAt = DateTime.UtcNow;
        var generation = await imageGenerationService.GenerateImageAsync(prompt, cancellationToken);

        var now = DateTime.UtcNow;
        var visualAssetId = Guid.NewGuid();

        var job = new AiJob
        {
            Id = Guid.NewGuid(),
            BrandProfileId = brand?.Id,
            TriggeredBy = userId,
            JobType = AiJobType.ImageGeneration,
            Status = generation.Succeeded ? AiJobStatus.Completed : AiJobStatus.Failed,
            InputParams = JsonSerializer.Serialize(new { prompt }),
            OutputRefId = generation.Succeeded ? visualAssetId : null,
            OutputRefType = "visual_asset",
            ErrorMessage = generation.ErrorMessage,
            StartedAt = startedAt,
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

        // NOTE: there is no "edit"/"regenerate" endpoint for images today — the content-gen page's
        // edit flow just calls this generate endpoint again, producing an independent VisualAsset
        // row with no relationship to the previous one (VersionOf is never set). That means the
        // previous asset's Cloudinary upload is never cleaned up via mediaStorageService.DeleteAsync
        // — a real "replace" endpoint would need to look up the prior asset's PublicId and delete it
        // once the new upload succeeds.
        var visualAsset = new VisualAsset
        {
            Id = visualAssetId,
            ContentItemId = request.ContentItemId,
            CampaignId = campaign?.Id,
            BrandProfileId = brand?.Id,
            TenantId = tenantId,
            GenerationMode = generationMode,
            Type = request.Type,
            SourceType = VisualAssetSourceType.AiGenerated,
            AiPrompt = prompt,
            Format = generation.ContentType?.Split('/').Last(),
            IsApproved = false,
            CreatedAt = now
        };

        // Cloudinary is the production path; falling back to an in-DB base64 data URI when it
        // isn't configured (local dev with no credentials) keeps the feature usable rather than
        // hard-failing every generation, mirroring IPublicImageHostingService.IsConfigured.
        if (mediaStorageService.IsConfigured)
        {
            var upload = await mediaStorageService.UploadImageAsync(
                generation.ImageBytes!, generation.ContentType ?? "image/jpeg", $"visual-assets/{tenantId}", cancellationToken);
            visualAsset.FileUrl = upload.Url;
            visualAsset.PublicId = upload.PublicId;
            visualAsset.WidthPx = upload.WidthPx;
            visualAsset.HeightPx = upload.HeightPx;
            visualAsset.FileSizeBytes = upload.FileSizeBytes;
            visualAsset.MimeType = upload.MimeType;
            visualAsset.StorageProvider = "Cloudinary";
            visualAsset.UploadedAt = now;
        }
        else
        {
            visualAsset.FileUrl = $"data:{generation.ContentType};base64,{Convert.ToBase64String(generation.ImageBytes!)}";
        }

        dbContext.VisualAssets.Add(visualAsset);

        if (usesFreeTrial)
        {
            tenant.FreeImageGenerationsRemaining--;
        }
        else
        {
            await CoinPolicy.TrySpendAsync(dbContext, tenantId, userId, role, coinCost, cancellationToken, reason: "visual_generation");
        }

        var visualSubject = campaign is not null ? $"for \"{campaign.Name}\"" : brand is not null ? $"for {brand.Name}" : "(standalone)";
        NotificationPublisher.Notify(
            dbContext,
            userId,
            brand?.Id,
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

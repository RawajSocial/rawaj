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

namespace Rawaj.Application.Features.Content.GenerateContentItem;

public class GenerateContentItemCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    ICurrentTenantContext currentTenantContext,
    IAiTextGenerationService textGenerationService)
    : IRequestHandler<GenerateContentItemCommand, Result<GenerateContentItemResponse>>
{
    public async Task<Result<GenerateContentItemResponse>> Handle(
        GenerateContentItemCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;
        var userId = currentUserService.UserId!.Value;

        var brand = await dbContext.TenantBrandProfiles
            .FirstOrDefaultAsync(b => b.Id == request.BrandProfileId && b.TenantId == tenantId, cancellationToken);
        if (brand is null)
        {
            return Result<GenerateContentItemResponse>.Failure("Brand profile not found.");
        }

        MarketingCampaign? campaign = null;
        if (request.CampaignId.HasValue)
        {
            campaign = await dbContext.MarketingCampaigns
                .FirstOrDefaultAsync(c => c.Id == request.CampaignId.Value && c.BrandProfileId == brand.Id, cancellationToken);
            if (campaign is null)
            {
                return Result<GenerateContentItemResponse>.Failure("Campaign not found.");
            }
        }

        var creditsUsage = await AiCreditsPolicy.GetUsageAsync(dbContext, tenantId, cancellationToken);
        if (!creditsUsage.HasCreditsRemaining)
        {
            return Result<GenerateContentItemResponse>.Failure(
                $"Your subscription plan allows {creditsUsage.MaxCreditsMonthly} AI credits per month. Upgrade for more.");
        }

        var prompt = ContentPromptBuilder.BuildTextPrompt(
            brand,
            campaign,
            request.ContentType.ToString(),
            request.Platform.ToString(),
            request.Language.ToString(),
            request.Tone,
            request.AdditionalInstructions,
            request.TemplateStyle);

        var generation = await textGenerationService.GenerateTextAsync(prompt, cancellationToken);

        var now = DateTime.UtcNow;
        var contentItemId = Guid.NewGuid();

        var job = new AiJob
        {
            Id = Guid.NewGuid(),
            BrandProfileId = brand.Id,
            TriggeredBy = userId,
            JobType = AiJobType.ContentGeneration,
            Status = generation.Succeeded ? AiJobStatus.Completed : AiJobStatus.Failed,
            InputParams = JsonSerializer.Serialize(new { prompt }),
            OutputRefId = generation.Succeeded ? contentItemId : null,
            OutputRefType = "content_item",
            Tokens = generation.TokensUsed,
            ErrorMessage = generation.ErrorMessage,
            StartedAt = now,
            CompletedAt = now,
            CreatedAt = now
        };
        dbContext.AiJobs.Add(job);

        if (!generation.Succeeded)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result<GenerateContentItemResponse>.Failure(
                generation.ErrorMessage ?? "Content generation failed. Please try again.");
        }

        var contentItem = new ContentItem
        {
            Id = contentItemId,
            CampaignId = campaign?.Id,
            TenantId = tenantId,
            BrandProfileId = brand.Id,
            CreatedBy = userId,
            ContentType = request.ContentType,
            Platform = request.Platform,
            Language = request.Language,
            Content = generation.Text!,
            Tone = request.Tone,
            AiPromptUsed = prompt,
            Status = ContentStatus.Draft,
            CreatedAt = now,
            UpdatedAt = now
        };
        dbContext.ContentItems.Add(contentItem);

        var contentSubject = campaign is not null ? $"for \"{campaign.Name}\"" : $"for {brand.Name}";
        NotificationPublisher.Notify(
            dbContext,
            userId,
            brand.Id,
            NotificationType.Info,
            NotificationCategory.ReviewNeeded,
            "Content ready for review",
            $"A new {request.ContentType} draft {contentSubject} is ready for your review.",
            contentItem.Id,
            "content_item");

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<GenerateContentItemResponse>.Success(
            new GenerateContentItemResponse(contentItem.Id, campaign?.Id, contentItem.Content, contentItem.Status));
    }
}

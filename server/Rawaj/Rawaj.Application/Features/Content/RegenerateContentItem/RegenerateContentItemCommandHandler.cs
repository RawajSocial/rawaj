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

namespace Rawaj.Application.Features.Content.RegenerateContentItem;

/// <summary>
/// Regenerates a content item's copy from reviewer feedback, keeping a full history in
/// content_revisions (Previous/Current pair per revision) rather than overwriting silently.
/// </summary>
public class RegenerateContentItemCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    ICurrentTenantContext currentTenantContext,
    IAiTextGenerationService textGenerationService)
    : IRequestHandler<RegenerateContentItemCommand, Result<RegenerateContentItemResponse>>
{
    public async Task<Result<RegenerateContentItemResponse>> Handle(
        RegenerateContentItemCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;
        var userId = currentUserService.UserId!.Value;

        var contentItem = await dbContext.ContentItems
            .FirstOrDefaultAsync(c => c.Id == request.ContentItemId && c.TenantId == tenantId, cancellationToken);
        if (contentItem is null)
        {
            return Result<RegenerateContentItemResponse>.Failure("Content item not found.");
        }

        if (contentItem.Status == ContentStatus.Published)
        {
            return Result<RegenerateContentItemResponse>.Failure("Published content cannot be regenerated.");
        }

        if (contentItem.CampaignId is null)
        {
            return Result<RegenerateContentItemResponse>.Failure("Standalone trial content cannot be regenerated.");
        }

        var campaign = await dbContext.MarketingCampaigns
            .FirstAsync(c => c.Id == contentItem.CampaignId, cancellationToken);
        var brand = await dbContext.TenantBrandProfiles
            .FirstAsync(b => b.Id == campaign.BrandProfileId, cancellationToken);

        var creditsUsage = await AiCreditsPolicy.GetUsageAsync(dbContext, tenantId, cancellationToken);
        if (!creditsUsage.HasCreditsRemaining)
        {
            return Result<RegenerateContentItemResponse>.Failure(
                $"Your subscription plan allows {creditsUsage.MaxCreditsMonthly} AI credits per month. Upgrade for more.");
        }

        var prompt = ContentPromptBuilder.BuildRevisionPrompt(brand, campaign, contentItem, request.Feedback);
        var generation = await textGenerationService.GenerateTextAsync(prompt, cancellationToken);

        var now = DateTime.UtcNow;

        var job = new AiJob
        {
            Id = Guid.NewGuid(),
            BrandProfileId = brand.Id,
            TriggeredBy = userId,
            JobType = AiJobType.ContentGeneration,
            Status = generation.Succeeded ? AiJobStatus.Completed : AiJobStatus.Failed,
            InputParams = JsonSerializer.Serialize(new { prompt }),
            OutputRefId = generation.Succeeded ? contentItem.Id : null,
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
            return Result<RegenerateContentItemResponse>.Failure(
                generation.ErrorMessage ?? "Content regeneration failed. Please try again.");
        }

        var revisionNumber = await dbContext.ContentRevisions
            .Where(r => r.ContentItemId == contentItem.Id)
            .CountAsync(cancellationToken) + 1;

        dbContext.ContentRevisions.Add(new ContentRevision
        {
            Id = Guid.NewGuid(),
            ContentItemId = contentItem.Id,
            RevisedBy = userId,
            RevisionPrompt = request.Feedback,
            Previous = contentItem.Content,
            Current = generation.Text!,
            RevisionNumber = revisionNumber,
            CreatedAt = now
        });

        contentItem.Content = generation.Text!;
        contentItem.Status = ContentStatus.Draft;
        contentItem.ReviewedBy = null;
        contentItem.ReviewedAt = null;
        contentItem.UpdatedAt = now;

        NotificationPublisher.Notify(
            dbContext,
            userId,
            brand.Id,
            NotificationType.Info,
            NotificationCategory.ReviewNeeded,
            "Revised content ready for review",
            $"Revision #{revisionNumber} of your {contentItem.ContentType} for \"{campaign.Name}\" is ready for review.",
            contentItem.Id,
            "content_item");

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<RegenerateContentItemResponse>.Success(
            new RegenerateContentItemResponse(contentItem.Id, contentItem.Content, contentItem.Status, revisionNumber));
    }
}

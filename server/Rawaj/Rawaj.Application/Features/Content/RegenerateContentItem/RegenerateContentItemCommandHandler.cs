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
    IAiTextGenerationService textGenerationService,
    ICoinCostProvider coinCostProvider)
    : IRequestHandler<RegenerateContentItemCommand, Result<RegenerateContentItemResponse>>
{
    public async Task<Result<RegenerateContentItemResponse>> Handle(RegenerateContentItemCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;
        var userId = currentUserService.UserId!.Value;
        var role = currentTenantContext.Role!.Value;

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

        var campaign = contentItem.CampaignId.HasValue
            ? await dbContext.MarketingCampaigns.FirstOrDefaultAsync(c => c.Id == contentItem.CampaignId, cancellationToken)
            : null;
        var brand = await dbContext.TenantBrandProfiles
            .FirstAsync(b => b.Id == contentItem.BrandProfileId, cancellationToken);

        var creditsUsage = await AiCreditsPolicy.GetUsageAsync(dbContext, tenantId, cancellationToken);
        if (!creditsUsage.HasCreditsRemaining)
        {
            return Result<RegenerateContentItemResponse>.Failure(
                $"Your subscription plan allows {creditsUsage.MaxCreditsMonthly} AI credits per month. Upgrade for more.");
        }

        var coinCost = await CoinPricingPolicy.GetDiscountedCostAsync(dbContext, tenantId, coinCostProvider.ContentGeneration, cancellationToken);
        var coinBalance = await CoinPolicy.GetBalanceAsync(dbContext, tenantId, userId, role, cancellationToken);
        if (coinBalance < coinCost)
        {
            return Result<RegenerateContentItemResponse>.Failure(
                CoinPolicy.InsufficientCoinsMessage(coinCost, coinBalance, "regenerate content"));
        }

        var prompt = ContentPromptBuilder.BuildRevisionPrompt(brand, campaign, contentItem, request.Feedback);
        var startedAt = DateTime.UtcNow;
        var generation = await textGenerationService.GenerateTextAsync(prompt, cancellationToken);

        var now = DateTime.UtcNow;

        var job = new AiJob
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BrandProfileId = brand.Id,
            TriggeredBy = userId,
            JobType = AiJobType.ContentGeneration,
            Status = generation.Succeeded ? AiJobStatus.Completed : AiJobStatus.Failed,
            InputParams = JsonSerializer.Serialize(new { prompt }),
            OutputRefId = generation.Succeeded ? contentItem.Id : null,
            OutputRefType = "content_item",
            Tokens = generation.TokensUsed,
            ErrorMessage = generation.ErrorMessage,
            StartedAt = startedAt,
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

        await CoinPolicy.TrySpendAsync(dbContext, tenantId, userId, role, coinCost, cancellationToken, reason: "content_regeneration");

        var contentSubject = campaign is not null ? $"for \"{campaign.Name}\"" : $"for {brand.Name}";
        NotificationPublisher.Notify(
            dbContext,
            userId,
            brand.Id,
            NotificationType.Info,
            NotificationCategory.ReviewNeeded,
            "Revised content ready for review",
            $"Revision #{revisionNumber} of your {contentItem.ContentType} {contentSubject} is ready for review.",
            contentItem.Id,
            "content_item");

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<RegenerateContentItemResponse>.Success(
            new RegenerateContentItemResponse(contentItem.Id, contentItem.Content, contentItem.Status, revisionNumber));
    }
}

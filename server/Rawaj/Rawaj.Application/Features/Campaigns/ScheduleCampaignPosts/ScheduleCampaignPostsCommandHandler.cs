using Microsoft.EntityFrameworkCore;
using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Policies;
using Rawaj.Application.Features.Scheduling.Common;
using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.Entities.SocialMedia;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Campaigns.ScheduleCampaignPosts;

/// <summary>
/// Bulk-schedules every Approved, not-yet-scheduled content item in a campaign, routing each item
/// to the brand's connected active account matching that item's own platform - a campaign mixing
/// Instagram and Facebook posts lands on both, rather than forcing one caller-picked account for
/// everything. Reuses SchedulePostCommandHandler's exact per-post checks (plan cap, coin cost,
/// native scheduling handoff) one item at a time so a single failure doesn't cost or block the
/// rest of the batch - the response reports per-post success/failure/skip so the UI can show which
/// posts landed, which need attention, and which have no connected account yet.
/// </summary>
public class ScheduleCampaignPostsCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    ICurrentTenantContext currentTenantContext,
    ITokenEncryptor tokenEncryptor,
    IEnumerable<ISocialPublisher> publishers,
    ICoinCostProvider coinCostProvider)
    : IRequestHandler<ScheduleCampaignPostsCommand, Result<ScheduleCampaignPostsResponse>>
{
    public async Task<Result<ScheduleCampaignPostsResponse>> Handle(
        ScheduleCampaignPostsCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;
        var userId = currentUserService.UserId!.Value;
        var role = currentTenantContext.Role!.Value;

        var campaign = await dbContext.MarketingCampaigns
            .FirstOrDefaultAsync(c => c.Id == request.CampaignId && c.BrandProfile.TenantId == tenantId, cancellationToken);
        if (campaign is null)
        {
            return Result<ScheduleCampaignPostsResponse>.Failure("Campaign not found.");
        }

        var accounts = await dbContext.SocialAccounts
            .Where(s => s.BrandProfileId == campaign.BrandProfileId && s.IsActive)
            .ToListAsync(cancellationToken);

        if (accounts.Count == 0)
        {
            return Result<ScheduleCampaignPostsResponse>.Failure(
                "No social accounts are connected to this brand. Connect at least one account before scheduling.");
        }

        // One account per platform. A brand can in principle hold two active accounts on the same
        // platform (e.g. two Facebook pages); pick the most recently verified so routing is
        // deterministic rather than arbitrary.
        var accountByPlatform = accounts
            .GroupBy(s => s.Platform)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(s => s.LastVerifiedAt ?? s.CreatedAt).First());

        var alreadyScheduledContentItemIds = await dbContext.ScheduledPosts
            .Where(s => s.CampaignId == campaign.Id
                && (s.Status == ScheduledPostStatus.Pending || s.Status == ScheduledPostStatus.Published))
            .Select(s => s.ContentItemId)
            .ToListAsync(cancellationToken);

        var pendingItems = await dbContext.ContentItems
            .Where(c => c.CampaignId == campaign.Id
                && c.Status == ContentStatus.Approved
                && !alreadyScheduledContentItemIds.Contains(c.Id))
            .OrderBy(c => c.SuggestedPostAt ?? c.CreatedAt)
            .ToListAsync(cancellationToken);

        if (pendingItems.Count == 0)
        {
            return Result<ScheduleCampaignPostsResponse>.Failure("No approved, unscheduled posts to schedule for this campaign.");
        }

        var results = new List<ScheduleCampaignPostResult>();
        var ordinal = 0;

        foreach (var item in pendingItems)
        {
            if (!accountByPlatform.TryGetValue(item.Platform, out var account))
            {
                results.Add(new ScheduleCampaignPostResult(
                    item.Id, item.Platform, Succeeded: false, Skipped: true,
                    Error: $"No connected {item.Platform} account for this brand. Connect one, then schedule again.",
                    ScheduledPostId: null, ScheduledAt: null));
                continue;
            }

            results.Add(await ScheduleOneAsync(campaign.Id, item, account, tenantId, userId, role, ordinal, cancellationToken));
            ordinal++;
        }

        var succeededCount = results.Count(r => r.Succeeded);
        var skippedCount = results.Count(r => r.Skipped);
        var failedCount = results.Count - succeededCount - skippedCount;
        var missingPlatforms = results.Where(r => r.Skipped).Select(r => r.Platform).Distinct().ToList();

        if (succeededCount > 0)
        {
            var platforms = string.Join(", ", results.Where(r => r.Succeeded).Select(r => r.Platform).Distinct());
            NotificationPublisher.Notify(
                dbContext, userId, campaign.BrandProfileId,
                NotificationType.Success, NotificationCategory.System,
                "Campaign posts scheduled",
                $"{succeededCount} post(s) for \"{campaign.Name}\" were scheduled to {platforms}.",
                campaign.Id, "marketing_campaign");
        }

        if (skippedCount > 0)
        {
            NotificationPublisher.Notify(
                dbContext, userId, campaign.BrandProfileId,
                NotificationType.Warning, NotificationCategory.System,
                "Some campaign posts could not be scheduled",
                $"{skippedCount} post(s) for \"{campaign.Name}\" have no connected account on: {string.Join(", ", missingPlatforms)}.",
                campaign.Id, "marketing_campaign");
        }

        if (succeededCount > 0 || skippedCount > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return Result<ScheduleCampaignPostsResponse>.Success(
            new ScheduleCampaignPostsResponse(campaign.Id, succeededCount, failedCount, skippedCount, missingPlatforms, results));
    }

    private async Task<ScheduleCampaignPostResult> ScheduleOneAsync(
        Guid campaignId,
        ContentItem item,
        SocialAccount socialAccount,
        Guid tenantId,
        Guid userId,
        TenantMemberRole role,
        int ordinal,
        CancellationToken cancellationToken)
    {
        var maxScheduledPosts = await (
            from tenant in dbContext.Tenants
            join subscription in dbContext.Subscriptions on tenant.SubscriptionId equals subscription.Id
            join plan in dbContext.SubscriptionPlans on subscription.SubscriptionPlanId equals plan.Id
            where tenant.Id == tenantId
            select plan.MaxScheduledPosts
        ).FirstAsync(cancellationToken);

        var activeScheduledCount = await dbContext.ScheduledPosts
            .Where(s => s.ContentItem.TenantId == tenantId
                && (s.Status == ScheduledPostStatus.Pending || s.Status == ScheduledPostStatus.Published))
            .CountAsync(cancellationToken);

        if (activeScheduledCount >= maxScheduledPosts)
        {
            return new ScheduleCampaignPostResult(item.Id, item.Platform, false, false,
                CoinPolicy.ScheduledPostCapMessage(maxScheduledPosts), null, null);
        }

        var coinCost = await CoinPricingPolicy.GetDiscountedCostAsync(dbContext, tenantId, coinCostProvider.Scheduling, cancellationToken);
        var coinBalance = await CoinPolicy.GetBalanceAsync(dbContext, tenantId, userId, role, cancellationToken);
        if (coinBalance < coinCost)
        {
            return new ScheduleCampaignPostResult(item.Id, item.Platform, false, false,
                CoinPolicy.InsufficientCoinsMessage(coinCost, coinBalance, "schedule this post"), null, null);
        }

        var visualAssetId = await dbContext.VisualAssets
            .Where(v => v.ContentItemId == item.Id)
            .Select(v => (Guid?)v.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var scheduledAt = SchedulingWindow.ClampForward(item.SuggestedPostAt, now, ordinal);
        var aiSuggestedTime = item.SuggestedPostAt.HasValue && scheduledAt == item.SuggestedPostAt.Value;

        var scheduledPost = new ScheduledPost
        {
            Id = Guid.NewGuid(),
            ContentItemId = item.Id,
            VisualAssetId = visualAssetId,
            SocialAccountId = socialAccount.Id,
            BrandProfileId = item.BrandProfileId ?? socialAccount.BrandProfileId,
            CampaignId = campaignId,
            ScheduledAt = scheduledAt,
            AiSuggestedTime = aiSuggestedTime,
            Status = ScheduledPostStatus.Pending,
            RetryCount = 0,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.ScheduledPosts.Add(scheduledPost);
        await dbContext.SaveChangesAsync(cancellationToken);

        var handoffResult = await ScheduledPostPublisher.ScheduleNativelyAsync(
            dbContext, tokenEncryptor, publishers, scheduledPost.Id, scheduledPost.ScheduledAt, cancellationToken);

        if (!handoffResult.Succeeded)
        {
            // Keep the row instead of deleting it - ExecuteAsync already marks it Failed with the
            // real reason in most cases, but a defensive set here covers every failure branch, so
            // the post stays visible (as Failed) on the calendar/campaign pages instead of vanishing.
            scheduledPost.Status = ScheduledPostStatus.Failed;
            scheduledPost.ErrorMessage = handoffResult.ErrorMessage ?? "Could not schedule with the platform.";
            scheduledPost.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);

            return new ScheduleCampaignPostResult(item.Id, item.Platform, false, false,
                $"Could not schedule with {socialAccount.Platform}: {scheduledPost.ErrorMessage}", null, null);
        }

        await CoinPolicy.TrySpendAsync(dbContext, tenantId, userId, role, coinCost, cancellationToken, reason: "scheduling");
        await dbContext.SaveChangesAsync(cancellationToken);

        var post = handoffResult.Data!;
        return new ScheduleCampaignPostResult(item.Id, item.Platform, true, false, null, post.Id, post.ScheduledAt);
    }
}

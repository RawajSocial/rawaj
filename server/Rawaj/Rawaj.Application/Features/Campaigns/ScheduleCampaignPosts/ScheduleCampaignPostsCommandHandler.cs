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
/// Bulk-schedules every Approved, not-yet-scheduled content item in a campaign to one social
/// account, reusing SchedulePostCommandHandler's exact per-post checks (plan cap, coin cost,
/// native scheduling handoff) one item at a time so a single failure doesn't cost or block the
/// rest of the batch — the response reports per-post success/failure so the UI can show which
/// posts landed and which need attention.
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

        var socialAccount = await dbContext.SocialAccounts.FirstOrDefaultAsync(
            s => s.Id == request.SocialAccountId && s.BrandProfileId == campaign.BrandProfileId && s.IsActive,
            cancellationToken);
        if (socialAccount is null)
        {
            return Result<ScheduleCampaignPostsResponse>.Failure("Social account not found or not connected to this brand.");
        }

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

        foreach (var item in pendingItems)
        {
            var result = await ScheduleOneAsync(campaign.Id, item, socialAccount, tenantId, userId, role, cancellationToken);
            results.Add(result);
        }

        var succeededCount = results.Count(r => r.Succeeded);
        var failedCount = results.Count - succeededCount;

        if (succeededCount > 0)
        {
            NotificationPublisher.Notify(
                dbContext, userId, campaign.BrandProfileId,
                NotificationType.Success, NotificationCategory.System,
                "Campaign posts scheduled",
                $"{succeededCount} post(s) for \"{campaign.Name}\" were scheduled to {socialAccount.Platform}.",
                campaign.Id, "marketing_campaign");
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return Result<ScheduleCampaignPostsResponse>.Success(
            new ScheduleCampaignPostsResponse(campaign.Id, succeededCount, failedCount, results));
    }

    private async Task<ScheduleCampaignPostResult> ScheduleOneAsync(
        Guid campaignId,
        ContentItem item,
        SocialAccount socialAccount,
        Guid tenantId,
        Guid userId,
        TenantMemberRole role,
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
            return new ScheduleCampaignPostResult(item.Id, false,
                $"Your subscription plan allows a maximum of {maxScheduledPosts} scheduled post(s). Upgrade for more.", null, null);
        }

        var coinCost = await CoinPricingPolicy.GetDiscountedCostAsync(dbContext, tenantId, coinCostProvider.Scheduling, cancellationToken);
        var coinBalance = await CoinPolicy.GetBalanceAsync(dbContext, tenantId, userId, role, cancellationToken);
        if (coinBalance < coinCost)
        {
            return new ScheduleCampaignPostResult(item.Id, false,
                $"You need {coinCost} coins to schedule this post, but only have {coinBalance}.", null, null);
        }

        var visualAssetId = await dbContext.VisualAssets
            .Where(v => v.ContentItemId == item.Id)
            .Select(v => (Guid?)v.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var scheduledAt = item.SuggestedPostAt ?? now.AddHours(1);

        var scheduledPost = new ScheduledPost
        {
            Id = Guid.NewGuid(),
            ContentItemId = item.Id,
            VisualAssetId = visualAssetId,
            SocialAccountId = socialAccount.Id,
            BrandProfileId = item.BrandProfileId ?? socialAccount.BrandProfileId,
            CampaignId = campaignId,
            ScheduledAt = scheduledAt,
            AiSuggestedTime = item.SuggestedPostAt.HasValue,
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
            dbContext.ScheduledPosts.Remove(scheduledPost);
            await dbContext.SaveChangesAsync(cancellationToken);

            return new ScheduleCampaignPostResult(item.Id, false,
                $"Could not schedule with {socialAccount.Platform}: {handoffResult.ErrorMessage}", null, null);
        }

        await CoinPolicy.TrySpendAsync(dbContext, tenantId, userId, role, coinCost, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var post = handoffResult.Data!;
        return new ScheduleCampaignPostResult(item.Id, true, null, post.Id, post.ScheduledAt);
    }
}

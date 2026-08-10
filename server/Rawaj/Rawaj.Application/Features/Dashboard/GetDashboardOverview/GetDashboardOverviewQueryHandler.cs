using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Analytics.Common;
using Rawaj.Application.Features.Analytics.GetBrandAnalytics;

namespace Rawaj.Application.Features.Dashboard.GetDashboardOverview;

/// <summary>
/// Same "latest PostAnalytics snapshot per post" aggregation as GetBrandAnalyticsQueryHandler, but
/// scoped directly off ScheduledPost.BrandProfileId/CampaignId (denormalized columns) so an optional
/// CampaignId filter is trivial: null means every post for the brand, including standalone
/// (no-campaign) posts; a specific CampaignId narrows to just that campaign's posts.
///
/// <para>The four *ChangePercent fields compare each KPI's current value against the same
/// reduction reconstructed "as of" the start of the current calendar month - i.e. what the number
/// would have read at the end of last month - rather than any new-activity-this-period figure, so
/// they stay meaningful for a cumulative counter like PostsTracked without redefining what its own
/// headline number means.</para>
/// </summary>
public class GetDashboardOverviewQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetDashboardOverviewQuery, Result<DashboardOverviewResponse>>
{
    public async Task<Result<DashboardOverviewResponse>> Handle(GetDashboardOverviewQuery request, CancellationToken cancellationToken)
    {
        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var analyticsQuery = dbContext.PostAnalytics.Where(a =>
            a.ScheduledPost.BrandProfileId == request.BrandProfileId
            && (request.CampaignId == null || a.ScheduledPost.CampaignId == request.CampaignId));

        var latestPerPost = await PostAnalyticsAggregation.GetLatestPerPostAsync(analyticsQuery, cancellationToken);
        var priorPerPost = await PostAnalyticsAggregation.GetLatestPerPostAsync(analyticsQuery, cancellationToken, asOf: monthStart);

        // Follower counts live on SocialAccount, not on any post snapshot, and aren't scoped by
        // campaign (an account isn't tied to one campaign) - unioned with the post-derived
        // platforms below so a connected account with no synced posts yet still shows up.
        var accounts = await dbContext.SocialAccounts
            .Where(a => a.BrandProfileId == request.BrandProfileId && a.IsActive)
            .Select(a => new { a.Id, a.Platform, a.FollowerCount })
            .ToListAsync(cancellationToken);

        var followerCountsByPlatform = accounts
            .GroupBy(a => a.Platform)
            .ToDictionary(g => g.Key, g => (long)g.Sum(a => a.FollowerCount ?? 0));

        var accountIds = accounts.Select(a => a.Id).ToList();

        // The most recent follower snapshot at or before the start of this month, per account -
        // null (not 0) when an account has no snapshot that old yet, since the feature is new and
        // most accounts won't have a full month of history for a while.
        var priorFollowerSnapshots = accountIds.Count == 0
            ? []
            : await dbContext.FollowerCountSnapshots
                .Where(s => accountIds.Contains(s.SocialAccountId) && s.RecordedAt <= monthStart)
                .Where(s => s.RecordedAt == dbContext.FollowerCountSnapshots
                    .Where(s2 => s2.SocialAccountId == s.SocialAccountId && s2.RecordedAt <= monthStart)
                    .Max(s2 => s2.RecordedAt))
                .Select(s => s.FollowerCount)
                .ToListAsync(cancellationToken);

        var platformBreakdown = latestPerPost
            .Select(p => p.Platform)
            .Distinct()
            .Union(followerCountsByPlatform.Keys)
            .Select(platform => new PlatformBreakdownItem(
                platform,
                latestPerPost.Where(p => p.Platform == platform).Sum(p => p.UniqueViewers ?? 0),
                latestPerPost.Where(p => p.Platform == platform).Sum(p => p.Views ?? 0),
                followerCountsByPlatform.GetValueOrDefault(platform)))
            .OrderByDescending(p => p.UniqueViewers)
            .ToList();

        var topPosts = latestPerPost
            .OrderByDescending(p => p.UniqueViewers ?? 0)
            .Take(5)
            .Select(p => new TopPostItem(
                p.ScheduledPostId,
                p.ContentItemId,
                p.CampaignId,
                p.Platform,
                p.Title,
                p.Content,
                p.Views ?? 0,
                p.UniqueViewers ?? 0,
                p.Likes ?? 0,
                p.EngagementRate))
            .ToList();

        var bottomPosts = latestPerPost
            .OrderBy(p => p.UniqueViewers ?? 0)
            .Take(5)
            .Select(p => new TopPostItem(
                p.ScheduledPostId,
                p.ContentItemId,
                p.CampaignId,
                p.Platform,
                p.Title,
                p.Content,
                p.Views ?? 0,
                p.UniqueViewers ?? 0,
                p.Likes ?? 0,
                p.EngagementRate))
            .ToList();

        var engagementRate = PostAnalyticsAggregation.WeightedEngagementRate(latestPerPost);
        var priorEngagementRate = PostAnalyticsAggregation.WeightedEngagementRate(priorPerPost);

        var currentUniqueViewers = latestPerPost.Sum(p => p.UniqueViewers ?? 0);
        var priorUniqueViewers = priorPerPost.Sum(p => p.UniqueViewers ?? 0);

        var currentFollowers = followerCountsByPlatform.Values.Sum();
        long? priorFollowers = priorFollowerSnapshots.Count > 0 ? priorFollowerSnapshots.Sum() : null;

        var response = new DashboardOverviewResponse(
            request.BrandProfileId,
            request.CampaignId,
            latestPerPost.Count,
            latestPerPost.Sum(p => p.Views ?? 0),
            currentUniqueViewers,
            latestPerPost.Sum(p => p.Likes ?? 0),
            latestPerPost.Sum(p => p.Comments ?? 0),
            latestPerPost.Sum(p => p.Shares ?? 0),
            engagementRate,
            platformBreakdown,
            topPosts,
            bottomPosts,
            latestPerPost.Any(p => p.Views.HasValue),
            latestPerPost.Any(p => p.UniqueViewers.HasValue),
            engagementRate.HasValue,
            PercentChange(latestPerPost.Count, priorPerPost.Count),
            PercentChange(currentUniqueViewers, priorUniqueViewers),
            engagementRate.HasValue && priorEngagementRate.HasValue
                ? PercentChange(engagementRate.Value, priorEngagementRate.Value)
                : null,
            priorFollowers.HasValue ? PercentChange(currentFollowers, priorFollowers.Value) : null);

        return Result<DashboardOverviewResponse>.Success(response);
    }

    /// <summary>Null (not a numeric 0%) when there's nothing meaningful to compare against - a
    /// zero/unset prior value means "no prior data," not "no growth."</summary>
    private static decimal? PercentChange(decimal current, decimal prior)
    {
        if (prior <= 0) return null;
        return Math.Round((current - prior) / prior * 100, 1);
    }
}

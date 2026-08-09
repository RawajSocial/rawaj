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
/// </summary>
public class GetDashboardOverviewQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetDashboardOverviewQuery, Result<DashboardOverviewResponse>>
{
    public async Task<Result<DashboardOverviewResponse>> Handle(GetDashboardOverviewQuery request, CancellationToken cancellationToken)
    {
        var latestPerPost = await PostAnalyticsAggregation.GetLatestPerPostAsync(
            dbContext.PostAnalytics.Where(a =>
                a.ScheduledPost.BrandProfileId == request.BrandProfileId
                && (request.CampaignId == null || a.ScheduledPost.CampaignId == request.CampaignId)),
            cancellationToken);

        // Follower counts live on SocialAccount, not on any post snapshot, and aren't scoped by
        // campaign (an account isn't tied to one campaign) - unioned with the post-derived
        // platforms below so a connected account with no synced posts yet still shows up.
        var followerCountsByPlatform = await dbContext.SocialAccounts
            .Where(a => a.BrandProfileId == request.BrandProfileId && a.IsActive)
            .GroupBy(a => a.Platform)
            .Select(g => new { Platform = g.Key, FollowerCount = g.Sum(a => a.FollowerCount ?? 0) })
            .ToDictionaryAsync(x => x.Platform, x => (long)x.FollowerCount, cancellationToken);

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

        var response = new DashboardOverviewResponse(
            request.BrandProfileId,
            request.CampaignId,
            latestPerPost.Count,
            latestPerPost.Sum(p => p.Views ?? 0),
            latestPerPost.Sum(p => p.UniqueViewers ?? 0),
            latestPerPost.Sum(p => p.Likes ?? 0),
            latestPerPost.Sum(p => p.Comments ?? 0),
            latestPerPost.Sum(p => p.Shares ?? 0),
            engagementRate,
            platformBreakdown,
            topPosts,
            bottomPosts,
            latestPerPost.Any(p => p.Views.HasValue),
            latestPerPost.Any(p => p.UniqueViewers.HasValue),
            engagementRate.HasValue);

        return Result<DashboardOverviewResponse>.Success(response);
    }
}

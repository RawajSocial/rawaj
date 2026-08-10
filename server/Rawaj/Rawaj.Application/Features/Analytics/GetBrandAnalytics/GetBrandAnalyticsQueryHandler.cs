using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Analytics.Common;

namespace Rawaj.Application.Features.Analytics.GetBrandAnalytics;

/// <summary>
/// Mirrors GetCampaignAnalyticsQueryHandler's "latest snapshot per post" reduction, scoped to every
/// post across the whole brand instead of a single campaign - backs the dashboard overview page.
/// </summary>
public class GetBrandAnalyticsQueryHandler(IApplicationDbContext dbContext, ICurrentTenantContext currentTenantContext)
    : IRequestHandler<GetBrandAnalyticsQuery, Result<BrandAnalyticsOverview>>
{
    public async Task<Result<BrandAnalyticsOverview>> Handle(GetBrandAnalyticsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;

        var belongsToTenant = await dbContext.TenantBrandProfiles.AnyAsync(
            b => b.Id == request.BrandProfileId && b.TenantId == tenantId, cancellationToken);
        if (!belongsToTenant)
        {
            return Result<BrandAnalyticsOverview>.Failure("Brand profile not found.");
        }

        var latestPerPost = await PostAnalyticsAggregation.GetLatestPerPostAsync(
            dbContext.PostAnalytics.Where(a => a.ScheduledPost.ContentItem.BrandProfileId == request.BrandProfileId),
            cancellationToken);

        // Follower counts live on SocialAccount, not on any post snapshot - a brand can have a
        // connected account with a real follower count but zero synced posts yet, so this is
        // unioned with the post-derived platforms below rather than just joined onto them.
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

        var overview = new BrandAnalyticsOverview(
            request.BrandProfileId,
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

        return Result<BrandAnalyticsOverview>.Success(overview);
    }
}

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

        var platformBreakdown = latestPerPost
            .GroupBy(p => p.Platform)
            .Select(g => new PlatformBreakdownItem(g.Key, g.Sum(p => p.Reach ?? 0), g.Sum(p => p.Impressions ?? 0)))
            .OrderByDescending(p => p.Reach)
            .ToList();

        var topPosts = latestPerPost
            .OrderByDescending(p => p.Reach ?? 0)
            .Take(5)
            .Select(p => new TopPostItem(
                p.ScheduledPostId,
                p.ContentItemId,
                p.Platform,
                p.Title,
                p.Content,
                p.Reach ?? 0,
                p.Likes ?? 0,
                p.EngagementRate))
            .ToList();

        var overview = new BrandAnalyticsOverview(
            request.BrandProfileId,
            latestPerPost.Count,
            latestPerPost.Sum(p => p.Impressions ?? 0),
            latestPerPost.Sum(p => p.Reach ?? 0),
            latestPerPost.Sum(p => p.Likes ?? 0),
            latestPerPost.Sum(p => p.Comments ?? 0),
            latestPerPost.Sum(p => p.Shares ?? 0),
            PostAnalyticsAggregation.AverageEngagementRate(latestPerPost),
            platformBreakdown,
            topPosts);

        return Result<BrandAnalyticsOverview>.Success(overview);
    }
}

using MediatR;
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
                p.CampaignId,
                p.Platform,
                p.Title,
                p.Content,
                p.Reach ?? 0,
                p.Likes ?? 0,
                p.EngagementRate))
            .ToList();

        var response = new DashboardOverviewResponse(
            request.BrandProfileId,
            request.CampaignId,
            latestPerPost.Count,
            latestPerPost.Sum(p => p.Impressions ?? 0),
            latestPerPost.Sum(p => p.Reach ?? 0),
            latestPerPost.Sum(p => p.Likes ?? 0),
            latestPerPost.Sum(p => p.Comments ?? 0),
            latestPerPost.Sum(p => p.Shares ?? 0),
            PostAnalyticsAggregation.AverageEngagementRate(latestPerPost),
            platformBreakdown,
            topPosts);

        return Result<DashboardOverviewResponse>.Success(response);
    }
}

using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Analytics.Common;

namespace Rawaj.Application.Features.Analytics.GetCampaignAnalytics;

/// <summary>
/// Aggregates the latest analytics snapshot per post in a campaign via the shared
/// PostAnalyticsAggregation helper (also used by GetBrandAnalyticsQueryHandler and the Dashboard
/// query slice).
/// </summary>
public class GetCampaignAnalyticsQueryHandler(IApplicationDbContext dbContext, ICurrentTenantContext currentTenantContext)
    : IRequestHandler<GetCampaignAnalyticsQuery, Result<CampaignAnalyticsSummary>>
{
    public async Task<Result<CampaignAnalyticsSummary>> Handle(GetCampaignAnalyticsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;

        var belongsToTenant = await dbContext.MarketingCampaigns.AnyAsync(
            c => c.Id == request.CampaignId && c.BrandProfile.TenantId == tenantId, cancellationToken);
        if (!belongsToTenant)
        {
            return Result<CampaignAnalyticsSummary>.Failure("Campaign not found.");
        }

        var latestPerPost = await PostAnalyticsAggregation.GetLatestPerPostAsync(
            dbContext.PostAnalytics.Where(a => a.ScheduledPost.ContentItem.CampaignId == request.CampaignId),
            cancellationToken);

        var posts = latestPerPost
            .Select(p => new PostAnalyticsSnapshot(
                p.Id,
                p.ScheduledPostId,
                p.Platform,
                p.RecordedAt,
                p.Impressions,
                p.Reach,
                p.Likes,
                p.Comments,
                p.Shares,
                p.Saves,
                p.Clicks,
                p.EngagementRate))
            .ToList();

        var summary = new CampaignAnalyticsSummary(
            request.CampaignId,
            latestPerPost.Count,
            latestPerPost.Sum(p => p.Impressions ?? 0),
            latestPerPost.Sum(p => p.Reach ?? 0),
            latestPerPost.Sum(p => p.Likes ?? 0),
            latestPerPost.Sum(p => p.Comments ?? 0),
            latestPerPost.Sum(p => p.Shares ?? 0),
            PostAnalyticsAggregation.AverageEngagementRate(latestPerPost),
            posts);

        return Result<CampaignAnalyticsSummary>.Success(summary);
    }
}

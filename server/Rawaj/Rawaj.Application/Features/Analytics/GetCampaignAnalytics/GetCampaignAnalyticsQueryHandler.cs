using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Analytics.GetCampaignAnalytics;

/// <summary>
/// Aggregates the latest analytics snapshot per post in a campaign. Picking "latest per group" is
/// not translatable to a single SQL query against the JSON-free PostAnalytics table in a portable
/// way here, so snapshots are materialized first and the latest-per-post reduction happens
/// in-memory.
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

        var snapshots = await dbContext.PostAnalytics
            .Where(a => a.ScheduledPost.ContentItem.CampaignId == request.CampaignId)
            .OrderByDescending(a => a.RecordedAt)
            .Select(a => new PostAnalyticsSnapshot(
                a.Id,
                a.ScheduledPostId,
                a.Platform,
                a.RecordedAt,
                a.Impressions,
                a.Reach,
                a.Likes,
                a.Comments,
                a.Shares,
                a.Saves,
                a.Clicks,
                a.EngagementRate))
            .ToListAsync(cancellationToken);

        var latestPerPost = snapshots
            .GroupBy(a => a.ScheduledPostId)
            .Select(g => g.First())
            .ToList();

        var engagementRates = latestPerPost.Where(p => p.EngagementRate.HasValue).Select(p => p.EngagementRate!.Value).ToList();

        var summary = new CampaignAnalyticsSummary(
            request.CampaignId,
            latestPerPost.Count,
            latestPerPost.Sum(p => p.Impressions ?? 0),
            latestPerPost.Sum(p => p.Reach ?? 0),
            latestPerPost.Sum(p => p.Likes ?? 0),
            latestPerPost.Sum(p => p.Comments ?? 0),
            latestPerPost.Sum(p => p.Shares ?? 0),
            engagementRates.Count > 0 ? Math.Round(engagementRates.Average(), 4) : null,
            latestPerPost);

        return Result<CampaignAnalyticsSummary>.Success(summary);
    }
}

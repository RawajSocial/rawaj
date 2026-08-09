using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Analytics.Common;
using Rawaj.Domain.Enums;

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

        // Scoped via ScheduledPost.CampaignId (denormalized at schedule time) rather than
        // ScheduledPost.ContentItem.CampaignId, so this can never silently diverge from how the
        // Dashboard and GetScheduledPosts queries scope the same posts.
        var latestPerPost = await PostAnalyticsAggregation.GetLatestPerPostAsync(
            dbContext.PostAnalytics.Where(a => a.ScheduledPost.CampaignId == request.CampaignId),
            cancellationToken);

        var posts = latestPerPost
            .Select(p => new PostAnalyticsSnapshot(
                p.Id,
                p.ScheduledPostId,
                p.Platform,
                p.RecordedAt,
                p.Views,
                p.UniqueViewers,
                p.Likes,
                p.Comments,
                p.Shares,
                p.Saves,
                p.Clicks,
                p.EngagementRate))
            .ToList();

        // Counted from ScheduledPosts, not `latestPerPost.Count` (PostAnalytics) - a post that's
        // been published but never had its analytics synced yet would otherwise be invisible from
        // this count even though it's a real published post of this campaign.
        var postsTracked = await dbContext.ScheduledPosts
            .CountAsync(s => s.CampaignId == request.CampaignId && s.Status == ScheduledPostStatus.Published, cancellationToken);

        var summary = new CampaignAnalyticsSummary(
            request.CampaignId,
            postsTracked,
            latestPerPost.Sum(p => p.Views ?? 0),
            latestPerPost.Sum(p => p.UniqueViewers ?? 0),
            latestPerPost.Sum(p => p.Likes ?? 0),
            latestPerPost.Sum(p => p.Comments ?? 0),
            latestPerPost.Sum(p => p.Shares ?? 0),
            latestPerPost.Sum(p => p.Clicks ?? 0),
            PostAnalyticsAggregation.AverageEngagementRate(latestPerPost),
            posts,
            latestPerPost.Any(p => p.Views.HasValue),
            latestPerPost.Any(p => p.UniqueViewers.HasValue),
            latestPerPost.Any(p => p.EngagementRate.HasValue),
            latestPerPost.Any(p => p.Clicks.HasValue));

        return Result<CampaignAnalyticsSummary>.Success(summary);
    }
}

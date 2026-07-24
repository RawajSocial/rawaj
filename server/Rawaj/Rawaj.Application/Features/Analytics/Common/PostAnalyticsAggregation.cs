using Microsoft.EntityFrameworkCore;
using Rawaj.Domain.Entities.SocialMedia;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Analytics.Common;

/// <summary>
/// Shared "latest PostAnalytics snapshot per ScheduledPost" reduction, used by
/// GetBrandAnalyticsQueryHandler, GetCampaignAnalyticsQueryHandler, GetScheduledPostsQueryHandler
/// (My Ads per-post metrics) and the Dashboard query slice, so the aggregation logic lives in one
/// place instead of being reimplemented per consumer.
/// </summary>
public static class PostAnalyticsAggregation
{
    public record LatestPostSnapshot(
        Guid Id,
        Guid ScheduledPostId,
        Guid ContentItemId,
        Guid? CampaignId,
        SocialPlatform Platform,
        DateTime RecordedAt,
        long? Impressions,
        long? Reach,
        int? Likes,
        int? Comments,
        int? Shares,
        int? Saves,
        int? Clicks,
        decimal? EngagementRate,
        string? Title,
        string Content,
        DateTime ScheduledAt);

    /// <summary>
    /// Materializes every snapshot matching <paramref name="analyticsQuery"/> and reduces it to the
    /// most recently recorded snapshot per ScheduledPost. "Latest per group" isn't portably
    /// translatable to a single SQL query against this table, so the reduction happens in-memory
    /// after ordering by RecordedAt server-side.
    /// </summary>
    public static async Task<List<LatestPostSnapshot>> GetLatestPerPostAsync(
        IQueryable<PostAnalytics> analyticsQuery, CancellationToken cancellationToken)
    {
        var snapshots = await analyticsQuery
            .OrderByDescending(a => a.RecordedAt)
            .Select(a => new LatestPostSnapshot(
                a.Id,
                a.ScheduledPostId,
                a.ScheduledPost.ContentItemId,
                a.ScheduledPost.CampaignId,
                a.Platform,
                a.RecordedAt,
                a.Impressions,
                a.Reach,
                a.Likes,
                a.Comments,
                a.Shares,
                a.Saves,
                a.Clicks,
                a.EngagementRate,
                a.ScheduledPost.ContentItem.Title,
                a.ScheduledPost.ContentItem.Content,
                a.ScheduledPost.ScheduledAt))
            .ToListAsync(cancellationToken);

        return snapshots
            .GroupBy(a => a.ScheduledPostId)
            .Select(g => g.First())
            .ToList();
    }

    public static decimal? AverageEngagementRate(IReadOnlyCollection<LatestPostSnapshot> posts)
    {
        var rates = posts.Where(p => p.EngagementRate.HasValue).Select(p => p.EngagementRate!.Value).ToList();
        return rates.Count > 0 ? Math.Round(rates.Average(), 4) : null;
    }
}

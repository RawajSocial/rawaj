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
        long? Views,
        long? UniqueViewers,
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
                a.Views,
                a.UniqueViewers,
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

    /// <summary>
    /// Campaign/Brand/Dashboard-level Engagement Rate: <c>SUM(Engagements) / SUM(Views)</c> across
    /// every post in the group - never an average of each post's own <see cref="LatestPostSnapshot.EngagementRate"/>.
    /// Averaging per-post rates was the exact bug this replaces (KPI audit, analytics-spec.md §3): it
    /// lets a handful of high-rate, low-view posts skew the group figure far more than the actual
    /// engagement volume justifies. Null (not zero) when no post in the group has any recorded Views,
    /// keeping "unavailable" distinguishable from a genuine zero.
    /// </summary>
    public static decimal? WeightedEngagementRate(IReadOnlyCollection<LatestPostSnapshot> posts)
    {
        var totalViews = posts.Sum(p => p.Views ?? 0);
        if (totalViews <= 0)
        {
            return null;
        }

        var totalEngagements = posts.Sum(p => (p.Likes ?? 0) + (p.Comments ?? 0) + (p.Shares ?? 0));
        return Math.Round((decimal)totalEngagements / totalViews, 4);
    }
}

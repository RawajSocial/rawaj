using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Scheduling.GetScheduledPosts;

public record ScheduledPostSummary(
    Guid ScheduledPostId,
    Guid ContentItemId,
    Guid BrandProfileId,
    Guid? CampaignId,
    SocialPlatform Platform,
    string AccountName,
    DateTime ScheduledAt,
    ScheduledPostStatus Status,
    DateTime? PublishedAt,
    string? ErrorMessage,
    long? Views,
    long? UniqueViewers,
    int? Likes,
    int? Comments,
    int? Shares,
    int? Clicks,
    decimal? EngagementRate,
    /// <summary>The real post copy, joined from ContentItem — lets Ads/My Media show the
    /// actual content instead of a placeholder name.</summary>
    string Content,
    /// <summary>The specific visual asset attached at scheduling time, if any.</summary>
    string? ImageUrl,
    /// <summary>The platform's own id for the live post, set once actually published — lets the
    /// UI link straight to it (e.g. facebook.com/{PostId}) rather than just showing it happened.</summary>
    string? PostId);

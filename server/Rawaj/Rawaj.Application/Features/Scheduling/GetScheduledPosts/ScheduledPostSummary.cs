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
    string? ImageUrl);

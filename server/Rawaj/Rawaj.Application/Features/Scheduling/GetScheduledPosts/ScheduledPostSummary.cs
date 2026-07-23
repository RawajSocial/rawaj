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
    long? Impressions,
    long? Reach,
    int? Likes,
    int? Comments,
    int? Shares,
    int? Clicks,
    decimal? EngagementRate);

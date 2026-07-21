using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Analytics;

public record PostAnalyticsSnapshot(
    Guid Id,
    Guid ScheduledPostId,
    SocialPlatform Platform,
    DateTime RecordedAt,
    long? Impressions,
    long? Reach,
    int? Likes,
    int? Comments,
    int? Shares,
    int? Saves,
    int? Clicks,
    decimal? EngagementRate);

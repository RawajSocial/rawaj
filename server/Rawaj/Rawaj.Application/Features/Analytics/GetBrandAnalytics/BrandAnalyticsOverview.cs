using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Analytics.GetBrandAnalytics;

public record BrandAnalyticsOverview(
    Guid BrandProfileId,
    int PostsTracked,
    long TotalImpressions,
    long TotalReach,
    int TotalLikes,
    int TotalComments,
    int TotalShares,
    decimal? AverageEngagementRate,
    List<PlatformBreakdownItem> PlatformBreakdown,
    List<TopPostItem> TopPosts);

public record PlatformBreakdownItem(SocialPlatform Platform, long Reach, long Impressions);

public record TopPostItem(
    Guid ScheduledPostId,
    Guid ContentItemId,
    SocialPlatform Platform,
    string? Title,
    string Content,
    long Reach,
    int Likes,
    decimal? EngagementRate);

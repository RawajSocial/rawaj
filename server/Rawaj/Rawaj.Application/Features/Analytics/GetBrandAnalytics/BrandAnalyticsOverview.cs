using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Analytics.GetBrandAnalytics;

public record BrandAnalyticsOverview(
    Guid BrandProfileId,
    int PostsTracked,
    long TotalViews,
    long TotalUniqueViewers,
    int TotalLikes,
    int TotalComments,
    int TotalShares,
    decimal? AverageEngagementRate,
    List<PlatformBreakdownItem> PlatformBreakdown,
    List<TopPostItem> TopPosts);

public record PlatformBreakdownItem(SocialPlatform Platform, long UniqueViewers, long Views);

public record TopPostItem(
    Guid ScheduledPostId,
    Guid ContentItemId,
    Guid? CampaignId,
    SocialPlatform Platform,
    string? Title,
    string Content,
    long UniqueViewers,
    int Likes,
    decimal? EngagementRate);

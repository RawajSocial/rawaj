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
    List<TopPostItem> TopPosts,
    // Same shape as TopPosts, ascending instead of descending - derived from the same latest-per-post
    // reduction already computed for TopPosts, not a second query (analytics-spec.md §7).
    List<TopPostItem> BottomPosts,
    bool ViewsAvailable,
    bool UniqueViewersAvailable,
    bool EngagementRateAvailable);

public record PlatformBreakdownItem(SocialPlatform Platform, long UniqueViewers, long Views, long FollowerCount);

public record TopPostItem(
    Guid ScheduledPostId,
    Guid ContentItemId,
    Guid? CampaignId,
    SocialPlatform Platform,
    string? Title,
    string Content,
    long Views,
    long UniqueViewers,
    int Likes,
    decimal? EngagementRate);

using Rawaj.Application.Features.Analytics;
using Rawaj.Application.Features.Analytics.GetBrandAnalytics;

namespace Rawaj.Application.Features.Analytics.GetCampaignAnalytics;

public record CampaignAnalyticsSummary(
    Guid CampaignId,
    int PostsTracked,
    long TotalViews,
    long TotalUniqueViewers,
    int TotalLikes,
    int TotalComments,
    int TotalShares,
    int TotalClicks,
    // Likes + Comments + Shares summed across the campaign's latest-per-post snapshots
    // (analytics-spec.md §7) - the numerator CampaignEngagementRate below is derived from.
    long TotalEngagements,
    decimal? AverageEngagementRate,
    List<PostAnalyticsSnapshot> Posts,
    // Same TopPostItem shape as GetBrandAnalyticsQueryHandler/GetDashboardOverviewQueryHandler,
    // derived from the same latest-per-post reduction already computed above - no second query.
    List<TopPostItem> TopPosts,
    List<TopPostItem> BottomPosts,
    // Totals above are summed with `?? 0`, so an unsupported metric (e.g. Views/UniqueViewers before
    // read_insights is granted for an account) looks identical to a real zero. These flags let
    // clients tell "no data yet" apart from "not supported by this platform" without re-deriving it
    // from Posts themselves.
    bool ViewsAvailable,
    bool UniqueViewersAvailable,
    bool EngagementRateAvailable,
    bool ClicksAvailable);

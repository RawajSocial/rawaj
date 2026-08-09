using Rawaj.Application.Features.Analytics;

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
    decimal? AverageEngagementRate,
    List<PostAnalyticsSnapshot> Posts,
    // Totals above are summed with `?? 0`, so an unsupported metric (e.g. Views/UniqueViewers before
    // read_insights is granted for an account) looks identical to a real zero. These flags let
    // clients tell "no data yet" apart from "not supported by this platform" without re-deriving it
    // from Posts themselves.
    bool ViewsAvailable,
    bool UniqueViewersAvailable,
    bool EngagementRateAvailable,
    bool ClicksAvailable);

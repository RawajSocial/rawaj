using Rawaj.Application.Features.Analytics.GetBrandAnalytics;

namespace Rawaj.Application.Features.Dashboard.GetDashboardOverview;

public record DashboardOverviewResponse(
    Guid BrandProfileId,
    Guid? CampaignId,
    int PostsTracked,
    long TotalViews,
    long TotalUniqueViewers,
    int TotalLikes,
    int TotalComments,
    int TotalShares,
    decimal? AverageEngagementRate,
    List<PlatformBreakdownItem> PlatformBreakdown,
    List<TopPostItem> TopPosts,
    List<TopPostItem> BottomPosts,
    bool ViewsAvailable,
    bool UniqueViewersAvailable,
    bool EngagementRateAvailable,
    /// <summary>Percent change vs. the value as of the start of the current calendar month (i.e.
    /// "as of last month") - null when there's nothing to compare against yet (e.g. the prior value
    /// was 0/unset), not to be confused with a genuine 0% change.</summary>
    decimal? PostsTrackedChangePercent,
    decimal? TotalUniqueViewersChangePercent,
    decimal? AverageEngagementRateChangePercent,
    decimal? TotalFollowersChangePercent);

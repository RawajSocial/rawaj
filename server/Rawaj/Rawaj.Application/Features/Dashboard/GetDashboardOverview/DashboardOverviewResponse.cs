using Rawaj.Application.Features.Analytics.GetBrandAnalytics;

namespace Rawaj.Application.Features.Dashboard.GetDashboardOverview;

public record DashboardOverviewResponse(
    Guid BrandProfileId,
    Guid? CampaignId,
    int PostsTracked,
    long TotalImpressions,
    long TotalReach,
    int TotalLikes,
    int TotalComments,
    int TotalShares,
    decimal? AverageEngagementRate,
    List<PlatformBreakdownItem> PlatformBreakdown,
    List<TopPostItem> TopPosts);

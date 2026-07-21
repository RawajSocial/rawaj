using Rawaj.Application.Features.Analytics;

namespace Rawaj.Application.Features.Analytics.GetCampaignAnalytics;

public record CampaignAnalyticsSummary(
    Guid CampaignId,
    int PostsTracked,
    long TotalImpressions,
    long TotalReach,
    int TotalLikes,
    int TotalComments,
    int TotalShares,
    decimal? AverageEngagementRate,
    List<PostAnalyticsSnapshot> Posts);

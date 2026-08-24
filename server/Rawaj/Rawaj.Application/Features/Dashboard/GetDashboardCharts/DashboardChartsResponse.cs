namespace Rawaj.Application.Features.Dashboard.GetDashboardCharts;

public record DashboardChartsResponse(Guid BrandProfileId, Guid? CampaignId, List<DashboardChartPoint> Points);

public record DashboardChartPoint(DateOnly Date, long UniqueViewers, long Views, int Likes, int Comments, int Shares, decimal? EngagementRate);

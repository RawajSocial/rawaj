using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Dashboard.GetDashboardCharts;

/// <summary>
/// Time-series version of the brand/campaign analytics aggregation, for the dashboard's
/// BalanceChart. Unlike GetDashboardOverviewQueryHandler (which reduces PostAnalytics to one
/// "latest" snapshot per post), a chart needs every snapshot bucketed by the day it was recorded,
/// so this queries PostAnalytics directly instead of going through the latest-per-post helper.
/// </summary>
public class GetDashboardChartsQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetDashboardChartsQuery, Result<DashboardChartsResponse>>
{
    public async Task<Result<DashboardChartsResponse>> Handle(GetDashboardChartsQuery request, CancellationToken cancellationToken)
    {
        var days = Math.Clamp(request.Days, 1, 90);
        var since = DateTime.UtcNow.Date.AddDays(-(days - 1));

        var rows = await dbContext.PostAnalytics
            .Where(a =>
                a.ScheduledPost.BrandProfileId == request.BrandProfileId
                && (request.CampaignId == null || a.ScheduledPost.CampaignId == request.CampaignId)
                && a.RecordedAt >= since)
            .Select(a => new
            {
                a.RecordedAt,
                a.UniqueViewers,
                a.Views,
                a.Likes,
                a.Comments,
                a.Shares,
                a.EngagementRate,
            })
            .ToListAsync(cancellationToken);

        var grouped = rows
            .GroupBy(a => DateOnly.FromDateTime(a.RecordedAt.Date))
            .ToDictionary(
                g => g.Key,
                g => new DashboardChartPoint(
                    g.Key,
                    g.Sum(a => a.UniqueViewers ?? 0),
                    g.Sum(a => a.Views ?? 0),
                    g.Sum(a => a.Likes ?? 0),
                    g.Sum(a => a.Comments ?? 0),
                    g.Sum(a => a.Shares ?? 0),
                    g.Any(a => a.EngagementRate.HasValue)
                        ? Math.Round(g.Where(a => a.EngagementRate.HasValue).Average(a => a.EngagementRate!.Value), 4)
                        : null));

        var points = Enumerable.Range(0, days)
            .Select(offset => DateOnly.FromDateTime(since.AddDays(offset)))
            .Select(date => grouped.TryGetValue(date, out var point) ? point : new DashboardChartPoint(date, 0, 0, 0, 0, 0, null))
            .ToList();

        return Result<DashboardChartsResponse>.Success(new DashboardChartsResponse(request.BrandProfileId, request.CampaignId, points));
    }
}

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
        // 730 matches the frontend's largest range chip ("ALL", see FILTER_DAYS in balance-chart.ts) -
        // this used to cap at 90, silently flattening the 6M/1Y/ALL buttons to the same 90-day
        // window regardless of how much history actually existed.
        var days = Math.Clamp(request.Days, 1, 730);
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
                g =>
                {
                    // Weighted SUM(Engagements)/SUM(Views) for the day - the same fix Phase 4 applied
                    // to Campaign/Brand/Dashboard rollups, applied here to the per-day bucket. Averaging
                    // each snapshot's own EngagementRate (the prior behavior) let a handful of
                    // high-rate, low-view snapshots skew the day's figure the same way an unweighted
                    // per-post average did at the rollup level.
                    var dayViews = g.Sum(a => a.Views ?? 0);
                    var dayEngagementRate = dayViews > 0
                        ? Math.Round((decimal)g.Sum(a => (a.Likes ?? 0) + (a.Comments ?? 0) + (a.Shares ?? 0)) / dayViews, 4)
                        : (decimal?)null;

                    return new DashboardChartPoint(
                        g.Key,
                        g.Sum(a => a.UniqueViewers ?? 0),
                        dayViews,
                        g.Sum(a => a.Likes ?? 0),
                        g.Sum(a => a.Comments ?? 0),
                        g.Sum(a => a.Shares ?? 0),
                        dayEngagementRate);
                });

        var points = Enumerable.Range(0, days)
            .Select(offset => DateOnly.FromDateTime(since.AddDays(offset)))
            .Select(date => grouped.TryGetValue(date, out var point) ? point : new DashboardChartPoint(date, 0, 0, 0, 0, 0, null))
            .ToList();

        return Result<DashboardChartsResponse>.Success(new DashboardChartsResponse(request.BrandProfileId, request.CampaignId, points));
    }
}

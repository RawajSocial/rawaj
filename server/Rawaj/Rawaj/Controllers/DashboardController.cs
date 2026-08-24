using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Content.GetContentItems;
using Rawaj.Application.Features.Dashboard.GetDashboardActivity;
using Rawaj.Application.Features.Dashboard.GetDashboardCharts;
using Rawaj.Application.Features.Dashboard.GetDashboardOverview;
using Rawaj.Application.Features.Dashboard.GetDashboardRecentContent;
using Rawaj.Common;

namespace Rawaj.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/dashboard")]
public class DashboardController(ISender sender) : ControllerBase
{
    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview([FromQuery] Guid brandProfileId, [FromQuery] Guid? campaignId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetDashboardOverviewQuery(brandProfileId, campaignId), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<DashboardOverviewResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<DashboardOverviewResponse>.Fail(result.ErrorMessage!));
    }

    [HttpGet("charts")]
    public async Task<IActionResult> GetCharts([FromQuery] Guid brandProfileId, [FromQuery] Guid? campaignId, [FromQuery] int days = 14, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetDashboardChartsQuery(brandProfileId, campaignId, days), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<DashboardChartsResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<DashboardChartsResponse>.Fail(result.ErrorMessage!));
    }

    [HttpGet("recent-content")]
    public async Task<IActionResult> GetRecentContent([FromQuery] Guid brandProfileId, [FromQuery] Guid? campaignId, [FromQuery] int take = 10, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetDashboardRecentContentQuery(brandProfileId, campaignId, take), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<List<ContentItemSummary>>.Success(result.Data!))
            : BadRequest(ApiResponse<List<ContentItemSummary>>.Fail(result.ErrorMessage!));
    }

    [HttpGet("activity")]
    public async Task<IActionResult> GetActivity([FromQuery] Guid brandProfileId, [FromQuery] Guid? campaignId, [FromQuery] int take = 20, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetDashboardActivityQuery(brandProfileId, campaignId, take), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<List<DashboardActivityItem>>.Success(result.Data!))
            : BadRequest(ApiResponse<List<DashboardActivityItem>>.Fail(result.ErrorMessage!));
    }
}

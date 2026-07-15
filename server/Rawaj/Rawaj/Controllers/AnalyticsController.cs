using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rawaj.Application.Features.Analytics;
using Rawaj.Application.Features.Analytics.GetBrandAnalytics;
using Rawaj.Application.Features.Analytics.GetCampaignAnalytics;
using Rawaj.Application.Features.Analytics.GetPostAnalytics;
using Rawaj.Application.Features.Analytics.SyncPostAnalytics;
using Rawaj.Common;

namespace Rawaj.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/analytics")]
public class AnalyticsController(ISender sender) : ControllerBase
{
    [HttpPost("posts/{scheduledPostId:guid}/sync")]
    public async Task<IActionResult> SyncPost(Guid scheduledPostId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SyncPostAnalyticsCommand(scheduledPostId), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<PostAnalyticsSnapshot>.Success(result.Data!))
            : BadRequest(ApiResponse<PostAnalyticsSnapshot>.Fail(result.ErrorMessage!));
    }

    [HttpGet("posts/{scheduledPostId:guid}")]
    public async Task<IActionResult> GetPost(Guid scheduledPostId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPostAnalyticsQuery(scheduledPostId), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<List<PostAnalyticsSnapshot>>.Success(result.Data!))
            : BadRequest(ApiResponse<List<PostAnalyticsSnapshot>>.Fail(result.ErrorMessage!));
    }

    [HttpGet("campaigns/{campaignId:guid}")]
    public async Task<IActionResult> GetCampaign(Guid campaignId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCampaignAnalyticsQuery(campaignId), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<CampaignAnalyticsSummary>.Success(result.Data!))
            : BadRequest(ApiResponse<CampaignAnalyticsSummary>.Fail(result.ErrorMessage!));
    }

    [HttpGet("brand/{brandProfileId:guid}")]
    public async Task<IActionResult> GetBrand(Guid brandProfileId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetBrandAnalyticsQuery(brandProfileId), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<BrandAnalyticsOverview>.Success(result.Data!))
            : BadRequest(ApiResponse<BrandAnalyticsOverview>.Fail(result.ErrorMessage!));
    }
}

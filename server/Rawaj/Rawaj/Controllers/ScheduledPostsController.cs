using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Scheduling.CancelScheduledPost;
using Rawaj.Application.Features.Scheduling.GetScheduledPosts;
using Rawaj.Application.Features.Scheduling.PublishScheduledPost;
using Rawaj.Application.Features.Scheduling.SchedulePost;
using Rawaj.Common;

namespace Rawaj.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/scheduled-posts")]
public class ScheduledPostsController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Schedule(SchedulePostCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<SchedulePostResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<SchedulePostResponse>.Fail(result.ErrorMessage!));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? campaignId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetScheduledPostsQuery(campaignId, page, pageSize), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<PagedResult<ScheduledPostSummary>>.Success(result.Data!))
            : BadRequest(ApiResponse<PagedResult<ScheduledPostSummary>>.Fail(result.ErrorMessage!));
    }

    [HttpPost("{scheduledPostId:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid scheduledPostId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CancelScheduledPostCommand(scheduledPostId), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<CancelScheduledPostResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<CancelScheduledPostResponse>.Fail(result.ErrorMessage!));
    }

    [HttpPost("{scheduledPostId:guid}/publish-now")]
    public async Task<IActionResult> PublishNow(Guid scheduledPostId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new PublishScheduledPostCommand(scheduledPostId), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<PublishScheduledPostResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<PublishScheduledPostResponse>.Fail(result.ErrorMessage!));
    }
}

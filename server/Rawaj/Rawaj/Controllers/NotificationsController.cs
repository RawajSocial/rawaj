using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Notifications;
using Rawaj.Application.Features.Notifications.GetNotifications;
using Rawaj.Application.Features.Notifications.MarkAllNotificationsRead;
using Rawaj.Application.Features.Notifications.MarkNotificationRead;
using Rawaj.Common;

namespace Rawaj.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/notifications")]
public class NotificationsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool unreadOnly, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetNotificationsQuery(unreadOnly, page, pageSize), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<PagedResult<NotificationSummary>>.Success(result.Data!))
            : BadRequest(ApiResponse<PagedResult<NotificationSummary>>.Fail(result.ErrorMessage!));
    }

    [HttpPost("{notificationId:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid notificationId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new MarkNotificationReadCommand(notificationId), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<bool>.Success(result.Data))
            : BadRequest(ApiResponse<bool>.Fail(result.ErrorMessage!));
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new MarkAllNotificationsReadCommand(), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<int>.Success(result.Data))
            : BadRequest(ApiResponse<int>.Fail(result.ErrorMessage!));
    }
}

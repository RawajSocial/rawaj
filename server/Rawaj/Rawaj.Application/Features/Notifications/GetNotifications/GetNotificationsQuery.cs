using MediatR;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Notifications.GetNotifications;

public record GetNotificationsQuery(bool UnreadOnly, int Page = 1, int PageSize = 20)
    : IRequest<Result<PagedResult<NotificationSummary>>>;

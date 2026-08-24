using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Notifications.GetNotifications;

public class GetNotificationsQueryHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    : IRequestHandler<GetNotificationsQuery, Result<PagedResult<NotificationSummary>>>
{
    public async Task<Result<PagedResult<NotificationSummary>>> Handle(GetNotificationsQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId!.Value;
        var (page, pageSize) = PaginationDefaults.Clamp(request.Page, request.PageSize);

        var query = dbContext.Notifications
            .Where(n => n.UserId == userId)
            .Where(n => !request.UnreadOnly || !n.IsRead)
            .OrderByDescending(n => n.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var notifications = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationSummary(
                n.Id, n.Type, n.Category, n.Title, n.Message, n.RefId, n.RefType, n.IsRead, n.ReadAt, n.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<NotificationSummary>>.Success(new PagedResult<NotificationSummary>(notifications, page, pageSize, totalCount));
    }
}

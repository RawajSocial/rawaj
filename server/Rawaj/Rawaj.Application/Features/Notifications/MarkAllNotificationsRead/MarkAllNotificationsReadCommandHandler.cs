using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Notifications.MarkAllNotificationsRead;

public class MarkAllNotificationsReadCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    : IRequestHandler<MarkAllNotificationsReadCommand, Result<int>>
{
    public async Task<Result<int>> Handle(MarkAllNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId!.Value;
        var now = DateTime.UtcNow;

        var unread = await dbContext.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(cancellationToken);

        foreach (var notification in unread)
        {
            notification.IsRead = true;
            notification.ReadAt = now;
        }

        if (unread.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return Result<int>.Success(unread.Count);
    }
}

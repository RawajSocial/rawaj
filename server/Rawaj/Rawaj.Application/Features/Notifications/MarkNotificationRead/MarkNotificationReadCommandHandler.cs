using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Notifications.MarkNotificationRead;

public class MarkNotificationReadCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    : IRequestHandler<MarkNotificationReadCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId!.Value;

        var notification = await dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == request.NotificationId && n.UserId == userId, cancellationToken);
        if (notification is null)
        {
            return Result<bool>.Failure("Notification not found.");
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return Result<bool>.Success(true);
    }
}

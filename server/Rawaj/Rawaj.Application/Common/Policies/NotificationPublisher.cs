using Rawaj.Application.Common.Interfaces;
using Rawaj.Domain.Entities.Platform;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Common.Policies;

/// <summary>
/// Queues an in-app notification row. Callers add to the same dbContext they already have open
/// and rely on their own subsequent SaveChangesAsync - this never saves on its own, matching the
/// single-SaveChanges-per-request pattern used elsewhere (e.g. AiJob + ContentItem together).
/// </summary>
public static class NotificationPublisher
{
    public static void Notify(
        IApplicationDbContext dbContext,
        Guid userId,
        Guid? brandProfileId,
        NotificationType type,
        NotificationCategory category,
        string title,
        string message,
        Guid? refId = null,
        string? refType = null)
    {
        dbContext.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            BrandProfileId = brandProfileId,
            Type = type,
            Category = category,
            Title = title,
            Message = message,
            RefId = refId,
            RefType = refType,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });
    }
}

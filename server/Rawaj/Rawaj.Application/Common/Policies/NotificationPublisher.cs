using System.Text.Json;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Domain.Entities.Platform;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Common.Policies;

/// <summary>The outbox payload shape for a "NotificationCreated" delivery — everything a delivery
/// channel (SignalR today; email/push/webhook later) needs without a second DB round-trip.</summary>
public record NotificationCreatedPayload(
    Guid NotificationId,
    Guid UserId,
    string Type,
    string Category,
    string Title,
    string Message,
    Guid? RefId,
    string? RefType,
    DateTime CreatedAt);

/// <summary>
/// Queues an in-app notification row, plus an OutboxMessage referencing it so a background
/// dispatcher can reliably deliver it (SignalR broadcast today) even if the process crashes right
/// after this SaveChangesAsync commits. Callers add to the same dbContext they already have open
/// and rely on their own subsequent SaveChangesAsync - this never saves on its own, matching the
/// single-SaveChanges-per-request pattern used elsewhere (e.g. AiJob + ContentItem together) — the
/// Notification and OutboxMessage rows always commit together, atomically.
/// </summary>
public static class NotificationPublisher
{
    public const string NotificationCreatedType = "NotificationCreated";

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
        var now = DateTime.UtcNow;
        var notification = new Notification
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
            CreatedAt = now
        };
        dbContext.Notifications.Add(notification);

        var payload = new NotificationCreatedPayload(
            notification.Id, userId, type.ToString(), category.ToString(), title, message, refId, refType, now);

        dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = NotificationCreatedType,
            PayloadJson = JsonSerializer.Serialize(payload),
            CreatedAt = now
        });
    }
}

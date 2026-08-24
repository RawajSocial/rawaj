using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Notifications;

public record NotificationSummary(
    Guid Id,
    NotificationType Type,
    NotificationCategory Category,
    string Title,
    string Message,
    Guid? RefId,
    string? RefType,
    bool IsRead,
    DateTime? ReadAt,
    DateTime CreatedAt);

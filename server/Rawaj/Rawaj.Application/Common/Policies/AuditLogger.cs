using Rawaj.Application.Common.Interfaces;
using Rawaj.Domain.Entities.Platform;

namespace Rawaj.Application.Common.Policies;

/// <summary>
/// Queues an audit-log row, mirroring <see cref="NotificationPublisher"/>'s pattern — adds to the
/// caller's already-open dbContext and never saves on its own, so it composes with whatever the
/// handler was already going to SaveChangesAsync.
/// </summary>
public static class AuditLogger
{
    public static void Log(
        IApplicationDbContext dbContext,
        Guid? tenantId,
        Guid? userId,
        string action,
        string? message = null,
        string? entityType = null,
        Guid? entityId = null,
        Guid? brandProfileId = null,
        string? oldValue = null,
        string? newValue = null)
    {
        dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            BrandProfileId = brandProfileId,
            Action = action,
            Message = message,
            EntityType = entityType,
            EntityId = entityId,
            OldValue = oldValue,
            NewValue = newValue,
            CreatedAt = DateTime.UtcNow
        });
    }
}

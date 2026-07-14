using Rawaj.Domain.Common;
using Rawaj.Domain.Enums;

namespace Rawaj.Domain.Entities.Platform;

public class Notification : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid? BrandProfileId { get; set; }
    public NotificationType Type { get; set; }
    public NotificationCategory Category { get; set; }
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public Guid? RefId { get; set; }
    public string? RefType { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
}

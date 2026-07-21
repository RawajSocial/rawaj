using Rawaj.Domain.Common;

namespace Rawaj.Domain.Entities.Platform;

public class AuditLog : BaseEntity
{
    public Guid? BrandProfileId { get; set; }
    public Guid? UserId { get; set; }
    public string Action { get; set; } = null!;
    public string? Message { get; set; }
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
}

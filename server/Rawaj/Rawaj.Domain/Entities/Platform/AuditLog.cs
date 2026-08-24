using Rawaj.Domain.Common;

namespace Rawaj.Domain.Entities.Platform;

public class AuditLog : BaseEntity
{
    public Guid? TenantId { get; set; }
    public Guid? BrandProfileId { get; set; }
    public Guid? UserId { get; set; }
    public string Action { get; set; } = null!;
    public string? Message { get; set; }
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Metadata { get; set; }
    /// <summary>Structured before/after values for a change (e.g. a role or coin-allocation edit) —
    /// distinct from the free-text Message, which stays human-readable summary prose.</summary>
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime CreatedAt { get; set; }
}

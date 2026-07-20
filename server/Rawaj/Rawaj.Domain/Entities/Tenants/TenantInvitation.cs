using Rawaj.Domain.Common;
using Rawaj.Domain.Enums;

namespace Rawaj.Domain.Entities.Tenants;

public class TenantInvitation : BaseEntity
{
    public Guid TenantId { get; set; }
    public string Email { get; set; } = null!;
    public TenantMemberRole Role { get; set; }
    public Guid InvitedBy { get; set; }
    public InvitationStatus Status { get; set; }
    public DateTime? RespondedAt { get; set; }
    public DateTime ExpiresAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
}

using Rawaj.Domain.Common;
using Rawaj.Domain.Enums;

namespace Rawaj.Domain.Entities.Tenants;

public class TenantMember : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public TenantMemberRole Role { get; set; }
    public Guid? InvitedBy { get; set; }
    public InvitationStatus InvitationStatus { get; set; }
    public DateTime? JoinedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
    public ICollection<TenantMemberBrandAccess> BrandAccesses { get; set; } = [];
}

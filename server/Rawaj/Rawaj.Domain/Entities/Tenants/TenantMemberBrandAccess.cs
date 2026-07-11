using Rawaj.Domain.Common;

namespace Rawaj.Domain.Entities.Tenants;

public class TenantMemberBrandAccess : BaseEntity
{
    public Guid TenantMemberId { get; set; }
    public Guid BrandProfileId { get; set; }

    public TenantMember TenantMember { get; set; } = null!;
    public TenantBrandProfile BrandProfile { get; set; } = null!;
}

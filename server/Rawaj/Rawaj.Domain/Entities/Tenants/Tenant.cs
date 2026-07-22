using Rawaj.Domain.Common;
using Rawaj.Domain.Enums;
using Rawaj.Domain.ValueObjects;

namespace Rawaj.Domain.Entities.Tenants;

public class Tenant : BaseEntity
{
    public string Name { get; set; } = null!;
    public string Subdomain { get; set; } = null!;
    public TenantType TenantType { get; set; }
    public Guid OwnerUserId { get; set; }
    public Guid SubscriptionId { get; set; }
    public bool IsActive { get; set; }
    public int CoinBalance { get; set; }
    public bool IsActivated { get; set; }
    public TenantProfile? TenantProfile { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<TenantBrandProfile> BrandProfiles { get; set; } = [];
    public ICollection<TenantMember> Members { get; set; } = [];
}

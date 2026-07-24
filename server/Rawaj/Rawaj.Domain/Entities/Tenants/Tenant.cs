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

    /// <summary>Add-on units purchased via <c>PurchaseAddOnCommand</c> — added on top of the
    /// active plan's <c>MaxBrands</c>/<c>MaxUsers</c> wherever those limits are checked.</summary>
    public int ExtraBrandsPurchased { get; set; }
    public int ExtraMarketeersPurchased { get; set; }

    /// <summary>New-tenant free trials (pricing sheet section 3): one free complete marketing
    /// strategy, and 5 free image/content generations each, consumed before any coins are charged.</summary>
    public bool FreeMarketingPlanUsed { get; set; }
    public int FreeImageGenerationsRemaining { get; set; }
    public int FreeContentGenerationsRemaining { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<TenantBrandProfile> BrandProfiles { get; set; } = [];
    public ICollection<TenantMember> Members { get; set; } = [];
}

using Rawaj.Domain.Common;
using Rawaj.Domain.ValueObjects;

namespace Rawaj.Domain.Entities.Tenants;

public class TenantAccountSetup : BaseEntity
{
    public Guid BrandProfileId { get; set; }
    public string AccountType { get; set; } = null!; // "agency" | "business" — matches frontend literal
    public string Country { get; set; } = null!;
    public string? City { get; set; }
    public SocialLinks? SocialLinks { get; set; }
    public AgencyDetails? AgencyDetails { get; set; }
    public BusinessDetails? BusinessDetails { get; set; }
    public DateTime UpdatedAt { get; set; }

    public TenantBrandProfile BrandProfile { get; set; } = null!;
}

using Rawaj.Domain.Common;
using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.Enums;
using Rawaj.Domain.ValueObjects;

namespace Rawaj.Domain.Entities.Tenants;

public class TenantBrandProfile : BaseEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public BrandVoice? BrandVoice { get; set; }
    public BrandProfileStatus Status { get; set; }
    public BrandInfo? BrandInfo { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
    public ICollection<MarketingCampaign> Campaigns { get; set; } = [];
    public ICollection<ContentItem> ContentItems { get; set; } = [];
    public ICollection<VisualAsset> VisualAssets { get; set; } = [];
}

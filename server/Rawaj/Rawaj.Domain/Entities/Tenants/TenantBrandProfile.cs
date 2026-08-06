using Rawaj.Domain.Common;
using Rawaj.Domain.Entities.AiOperations;
using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.Enums;
using Rawaj.Domain.ValueObjects;

namespace Rawaj.Domain.Entities.Tenants;

public class TenantBrandProfile : BaseEntity, IConcurrencyAware
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public BrandVoice? BrandVoice { get; set; }
    public BrandProfileStatus Status { get; set; }
    public BrandInfo? BrandInfo { get; set; }

    /// <summary>
    /// The brand's current AI analysis, cached here because it is the same answer for every campaign
    /// of this brand until <see cref="BrandInfo"/> changes. Today the raw identity fields are pasted
    /// into every prompt of every campaign and re-interpreted by the model each time — more
    /// expensive, and inconsistent between calls in a way a brand voice should never be.
    ///
    /// Deliberately not recomputed when the brand is edited: that would spend the tenant's coins
    /// with no user action to attribute the spend to. The artifact's InputHash is compared against
    /// the current BrandInfo on read, and a mismatch simply means the next campaign pays to refresh
    /// it.
    /// </summary>
    public Guid? CurrentBrandAnalysisArtifactId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    public Tenant Tenant { get; set; } = null!;
    public AiArtifact? CurrentBrandAnalysisArtifact { get; set; }
    public ICollection<MarketingCampaign> Campaigns { get; set; } = [];
    public ICollection<ContentItem> ContentItems { get; set; } = [];
    public ICollection<VisualAsset> VisualAssets { get; set; } = [];
}

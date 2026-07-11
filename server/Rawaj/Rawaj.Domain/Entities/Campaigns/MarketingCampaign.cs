using Rawaj.Domain.Common;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;

namespace Rawaj.Domain.Entities.Campaigns;

public class MarketingCampaign : BaseEntity
{
    public Guid BrandProfileId { get; set; }
    public Guid CreatedBy { get; set; }
    public string Name { get; set; } = null!;
    public string? Objective { get; set; }
    public List<string> TargetPlatforms { get; set; } = [];
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public decimal? BudgetAmount { get; set; }
    public string? BudgetCurrency { get; set; }
    public CampaignStatus Status { get; set; }
    public string? AiPlanJson { get; set; }
    public DateTime? AiGeneratedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public TenantBrandProfile BrandProfile { get; set; } = null!;
    public ICollection<ContentItem> ContentItems { get; set; } = [];
    public ICollection<VisualAsset> VisualAssets { get; set; } = [];
}

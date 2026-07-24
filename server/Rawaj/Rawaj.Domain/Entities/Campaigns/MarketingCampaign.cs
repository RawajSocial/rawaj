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

    /// <summary>The onboarding wizard's collected answers (campaign type, brief, brand identity,
    /// audience, strategy inputs, AI-chat follow-up answers) as opaque JSON — this is where that
    /// data finally lands server-side, rather than only ever living in the browser's localStorage.</summary>
    public string? BriefJson { get; set; }
    /// <summary>Campaign-scoped Tavily competitor research — a `{summary, competitors[], sources[]}`
    /// blob, kept alongside (not instead of) the brand-level RagDocument rows.</summary>
    public string? CompetitorResearchJson { get; set; }
    /// <summary>The AI "what we understood about your business" artifact (summary, SWOT, maturity,
    /// growth stage, readiness, risks, opportunities, missing information).</summary>
    public string? DiagnosisJson { get; set; }
    /// <summary>Stamped when the user approves the generated strategy — gates content generation,
    /// which must not run against a plan nobody has signed off on.</summary>
    public DateTime? PlanApprovedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public TenantBrandProfile BrandProfile { get; set; } = null!;
    public ICollection<ContentItem> ContentItems { get; set; } = [];
    public ICollection<VisualAsset> VisualAssets { get; set; } = [];
}

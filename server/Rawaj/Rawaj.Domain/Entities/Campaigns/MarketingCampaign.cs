using Rawaj.Domain.Common;
using Rawaj.Domain.Entities.AiOperations;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;

namespace Rawaj.Domain.Entities.Campaigns;

public class MarketingCampaign : BaseEntity, IConcurrencyAware
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

    /// <summary>The pipeline run currently producing this campaign's strategy and content, if any.
    /// A campaign can be run through the pipeline more than once over its life (regenerate after a
    /// rethink); this points at the latest, and the run rows themselves are the history.</summary>
    public Guid? CurrentPipelineRunId { get; set; }

    /// <summary>The exact strategy artifact <see cref="PlanApprovedAt"/> refers to. Approval used to
    /// be a bare timestamp against a plan column that refinement overwrote in place, so "which
    /// strategy did the user actually approve" had no answer once it had been refined again. Content
    /// generation executes this version specifically, not merely the most recent one.</summary>
    public Guid? ApprovedStrategyArtifactId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>EF concurrency token — a second concurrent update to the same campaign (e.g. two
    /// team members editing at once) throws <c>DbUpdateConcurrencyException</c> instead of
    /// silently overwriting one editor's change.</summary>
    public byte[] RowVersion { get; set; } = null!;

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    public TenantBrandProfile BrandProfile { get; set; } = null!;
    public AiPipelineRun? CurrentPipelineRun { get; set; }
    public AiArtifact? ApprovedStrategyArtifact { get; set; }
    public ICollection<ContentItem> ContentItems { get; set; } = [];
    public ICollection<VisualAsset> VisualAssets { get; set; } = [];
}

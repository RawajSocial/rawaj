using Rawaj.Domain.Common;
using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;

namespace Rawaj.Domain.Entities.AiOperations;

/// <summary>
/// One execution of the campaign AI pipeline — the state machine that replaces the current design,
/// where "the pipeline" exists only as three HTTP calls chained by hand in an Angular component and
/// stage completion is inferred from whether a JSON column on MarketingCampaign is non-null.
///
/// A run owns its <see cref="AiPipelineStage"/> rows and knows nothing about how any individual
/// stage works. Stages never invoke each other: each writes its artifact, marks itself Completed and
/// stops, and the orchestrator decides what becomes runnable next. That is what allows a stage to be
/// executed alone, as part of a full run, or as a resume after a crash, with no code difference
/// between the three.
///
/// See docs/AI_PIPELINE.md §5 for the state machine.
/// </summary>
public class AiPipelineRun : BaseEntity, IConcurrencyAware
{
    /// <summary>Denormalised from the brand deliberately. AiJob's tenant attribution goes through a
    /// nullable BrandProfileId, which makes per-tenant AI reporting impossible for rows without a
    /// brand and needlessly expensive for the rest; this column is the fix applied from the start.</summary>
    public Guid TenantId { get; set; }

    public Guid BrandProfileId { get; set; }

    /// <summary>Null for a brand-scoped run (e.g. computing a brand analysis outside any campaign).
    /// Every campaign pipeline run sets it.</summary>
    public Guid? CampaignId { get; set; }

    public Guid TriggeredBy { get; set; }

    public AiPipelineRunStatus Status { get; set; }

    /// <summary>The stage the run is currently on (or last acted on), denormalised so listing runs
    /// doesn't require loading their stages. Advisory only — the stage rows are the truth.</summary>
    public AiPipelineStageKind? CurrentStage { get; set; }

    /// <summary>Rollup of every charge made by this run's stages, for reporting. The per-stage
    /// CoinsCharged values are authoritative; this is their sum.</summary>
    public int TotalCoinsSpent { get; set; }

    /// <summary>Last failure to reach run level, for display. Per-stage errors stay on the stage —
    /// this is the one the user is shown when the whole run is Failed.</summary>
    public string? LastError { get; set; }

    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>Content-generation parameters, fixed for the run at <c>StartAsync</c> — the graph and
    /// <c>StageContext</c> carry no other run-level configuration, so <c>ContentPlanExecutor</c> and
    /// <c>ContentImageExecutor</c> read these directly off <see cref="StageContext.Run"/> rather than
    /// through a separate parameters object. Only those two stages read them; everything upstream of
    /// content generation ignores them entirely. Defaults match what the placeholder constants in
    /// <c>ContentPlanExecutor</c> used before this run-level surface existed.</summary>
    public int ContentPostCount { get; set; } = 8;
    public Language ContentLanguage { get; set; } = Language.Ar;
    public bool ContentIncludeImages { get; set; } = true;
    public ContentTemplateStyle ContentTemplateStyle { get; set; } = ContentTemplateStyle.Auto;

    /// <summary>EF concurrency token, matching MarketingCampaign. The worker and an interactive
    /// request (approve, cancel, run-one-stage) can touch the same run concurrently; without this
    /// one would silently overwrite the other's status transition.</summary>
    public byte[] RowVersion { get; set; } = null!;

    public Tenant Tenant { get; set; } = null!;
    public TenantBrandProfile BrandProfile { get; set; } = null!;
    public MarketingCampaign? Campaign { get; set; }
    public ICollection<AiPipelineStage> Stages { get; set; } = [];
}

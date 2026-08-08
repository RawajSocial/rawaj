using Rawaj.Domain.Common;
using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;

namespace Rawaj.Domain.Entities.AiOperations;

/// <summary>
/// The typed, versioned output of a pipeline stage. Replaces the current design's opaque JSON columns
/// on MarketingCampaign (CompetitorResearchJson, DiagnosisJson, AiPlanJson) as the source of truth —
/// those columns are still written, but as projections, so every existing reader keeps working.
///
/// Two things this gives us that a column cannot. Versioning: refining a strategy adds v2 instead of
/// destroying v1, so an approval can point at a specific version and a user can be shown what
/// changed. And provenance: every artifact knows which stage produced it and from what inputs, so
/// "why does the strategy say this" is answerable.
///
/// <see cref="ContentJson"/> is only ever written after the payload has been validated against the
/// schema for its <see cref="Kind"/> (docs/AI_PIPELINE.md §4). An artifact that does not validate is
/// not stored and its stage is failed — storing unreadable text would pass every "does this exist"
/// check while rendering as a blank page to the user who paid for it, which is exactly the failure
/// AiJsonResponseParser was introduced to stop.
/// </summary>
public class AiArtifact : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid BrandProfileId { get; set; }

    /// <summary>Null for brand-scoped artifacts — BrandAnalysis belongs to the brand, not to whichever
    /// campaign happened to trigger its computation, which is what lets every later campaign of that
    /// brand reuse it.</summary>
    public Guid? CampaignId { get; set; }

    public AiArtifactKind Kind { get; set; }

    /// <summary>1-based, incremented per (CampaignId, Kind) — or (BrandProfileId, Kind) for
    /// brand-scoped artifacts.</summary>
    public int Version { get; set; }

    /// <summary>Exactly one version per scope+kind carries this. Readers that just want "the strategy"
    /// filter on it; the approval flow addresses a specific version instead.</summary>
    public bool IsCurrent { get; set; }

    /// <summary>Validated JSON only — see the class remarks.</summary>
    public string ContentJson { get; set; } = null!;

    /// <summary>Version of the schema <see cref="ContentJson"/> was validated against, so a future
    /// shape change can migrate or reject old artifacts explicitly rather than failing at read time
    /// on a missing property.</summary>
    public int SchemaVersion { get; set; }

    public Guid? SourceStageId { get; set; }

    /// <summary>The producing stage's InputHash, copied here so cache lookups (notably "is this
    /// brand's analysis still valid for its current BrandInfo") don't have to join back through the
    /// stage and its run.</summary>
    public string? InputHash { get; set; }

    public DateTime CreatedAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
    public TenantBrandProfile BrandProfile { get; set; } = null!;
    public MarketingCampaign? Campaign { get; set; }
    public AiPipelineStage? SourceStage { get; set; }
}

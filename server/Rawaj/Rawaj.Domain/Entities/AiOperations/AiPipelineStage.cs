using Rawaj.Domain.Common;
using Rawaj.Domain.Enums;

namespace Rawaj.Domain.Entities.AiOperations;

/// <summary>
/// One stage of one <see cref="AiPipelineRun"/>. This row is what makes the pipeline diagnosable and
/// resumable: it records not just whether a stage produced output, but how many times it was tried,
/// what went wrong, what kind of wrong it was, whether a worker currently holds it, and whether it
/// has already been charged for.
///
/// Most stages have exactly one row per run. <see cref="AiPipelineStageKind.ContentImage"/> is the
/// exception — it fans out to one row per generated post, keyed by <see cref="TargetRefId"/>, which
/// is what allows a single post's image to be retried without regenerating the batch.
///
/// See docs/AI_PIPELINE.md §5–§6 for the state machine and the retry policy.
/// </summary>
public class AiPipelineStage : BaseEntity
{
    public Guid RunId { get; set; }

    public AiPipelineStageKind Kind { get; set; }

    /// <summary>Display order. Seeded from the numeric value of <see cref="Kind"/>; fan-out rows of
    /// the same kind share an ordinal and are ordered by CreatedAt within it.</summary>
    public int Ordinal { get; set; }

    public AiPipelineStageStatus Status { get; set; }

    /// <summary>An optional stage that exhausts its attempts becomes Skipped rather than Failed, and
    /// a Skipped stage satisfies dependencies exactly as a Completed one does — so the run continues.
    /// This makes today's "best-effort" research behaviour an explicit property of the stage instead
    /// of a convention buried in a handler's error branch.</summary>
    public bool IsOptional { get; set; }

    public int Attempts { get; set; }
    public int MaxAttempts { get; set; }

    /// <summary>Set on a retryable failure to now + backoff. The orchestrator will not select this
    /// stage before it passes, which is what keeps a failing provider from being hammered.</summary>
    public DateTime? NextAttemptAt { get; set; }

    /// <summary>Identifies the worker instance currently holding this stage. Claiming is atomic
    /// (UPDATE ... OUTPUT ... WHERE Status = Pending) rather than read-then-write, because unlike the
    /// existing single-instance-safe pollers, a double-claim here would mean paying a provider twice
    /// for the same work.</summary>
    public string? LeaseOwner { get; set; }

    /// <summary>When the current claim expires. A worker that crashes mid-stage leaves the row
    /// Running forever without this; once it passes, the stage returns to Pending with its attempt
    /// count untouched — the work was never actually tried to completion.</summary>
    public DateTime? LeaseExpiresAt { get; set; }

    /// <summary>Hash over the canonical form of everything that feeds this stage: the ids and
    /// versions of its input artifacts, the relevant entity fields, and the prompt template version.
    ///
    /// Two uses. A Completed stage whose recomputed hash still matches is returned as-is — no model
    /// call, no charge — which is what makes re-invoking a single stage safe. And for BrandAnalysis,
    /// it is the cache key that lets one brand's analysis be reused by every campaign of that brand.
    ///
    /// Including the template version means shipping a changed prompt invalidates cached artifacts
    /// automatically, rather than serving output from a prompt that no longer exists.</summary>
    public string? InputHash { get; set; }

    public Guid? ArtifactId { get; set; }

    /// <summary>Coins actually charged for this stage, and the guard that makes charging idempotent:
    /// a retry, or a re-run of an already-completed stage, can never charge twice. Zero is the normal
    /// value for most stages — charging happens once per charge-point *group*, on the group's last
    /// stage, so that adding stages to the pipeline never raises the price of a run
    /// (docs/AI_PIPELINE.md §7).</summary>
    public int CoinsCharged { get; set; }

    public string? LastError { get; set; }

    /// <summary>What kind of failure the last one was, which decides the retry policy — a parse
    /// failure earns a repair re-ask, a coin shortfall consumes no attempt at all.</summary>
    public AiFailureKind? LastErrorKind { get; set; }

    /// <summary>What this stage acts on, for fan-out stages. ContentImage sets it to the
    /// ContentItemId whose image it generates; single-instance stages leave it null.</summary>
    public Guid? TargetRefId { get; set; }

    /// <summary>Entity name for <see cref="TargetRefId"/> (e.g. "content_item"), following the
    /// existing OutputRefType convention on AiJob.</summary>
    public string? TargetRefType { get; set; }

    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public AiPipelineRun Run { get; set; } = null!;
    public AiArtifact? Artifact { get; set; }

    /// <summary>The provider calls this stage made — plural, because one stage is not one call: the
    /// research stages issue a Tavily search and then a Groq synthesis, and a parse-repair re-ask is
    /// a second call to the same model. Pure stages (StrategyAssemble, HumanApproval) and cache hits
    /// make none.</summary>
    public ICollection<AiJob> AiJobs { get; set; } = [];
}

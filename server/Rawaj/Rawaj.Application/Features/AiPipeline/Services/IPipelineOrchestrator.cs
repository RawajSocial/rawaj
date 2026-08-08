using Rawaj.Domain.Entities.AiOperations;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.AiPipeline.Services;

/// <summary>
/// Starts, advances and cancels pipeline runs. See <see cref="PipelineOrchestrator"/>.
/// </summary>
public interface IPipelineOrchestrator
{
    /// <summary>Creates a run and its initial stage rows. A campaign run starts every non-fan-out
    /// stage in the graph (later ones simply wait on their dependencies); a brand-only run
    /// (<paramref name="campaignId"/> null) starts only <c>BrandAnalysis</c> — nothing else in the
    /// graph makes sense without a campaign to run it for.</summary>
    /// <param name="contentPostCount">How many posts <c>ContentPlan</c> should generate. Null keeps
    /// <see cref="AiPipelineRun"/>'s default (8) — every caller that isn't a legacy shim carrying a
    /// user-specified count.</param>
    /// <param name="contentLanguage">Null keeps the default (Arabic).</param>
    /// <param name="contentIncludeImages">Null keeps the default (true). False stops <c>ContentPlan</c>
    /// from fanning out to <c>ContentImage</c> stages at all — no VisualAsset rows, not even
    /// placeholders, matching what <c>GenerateCampaignContentCommandHandler</c> did when a caller
    /// opted out of images.</param>
    /// <param name="contentTemplateStyle">Null keeps the default (Auto).</param>
    Task<AiPipelineRun> StartAsync(
        Guid tenantId, Guid brandProfileId, Guid? campaignId, Guid triggeredBy, CancellationToken cancellationToken,
        int? contentPostCount = null, Language? contentLanguage = null,
        bool? contentIncludeImages = null, ContentTemplateStyle? contentTemplateStyle = null);

    /// <summary>
    /// Runs the graph forward until nothing more can happen without a person, a top-up, or time
    /// passing: dispatches every currently-runnable stage, settles each result (artifact + idempotent
    /// charge + Completed, or classified failure + backoff/Failed/Skipped), and repeats while the run
    /// stays <see cref="AiPipelineRunStatus.Running"/>. Safe to call repeatedly — on a fresh run, a
    /// parked one whose block has cleared, or one a stage's backoff has just passed for.
    /// </summary>
    /// <param name="role">The triggering user's role, needed wherever a stage charges coins. Not
    /// stored on the run itself — see the tracker's open items for why.</param>
    /// <param name="leaseOwner">Identifies whoever is calling — a worker instance id in production,
    /// a fixed default for an interactive/synchronous caller. Recorded as the stage's
    /// <c>LeaseOwner</c> at claim time, and is the value the atomic claim races on: two callers
    /// racing for the same <c>Pending</c> stage can only ever have one of them win the claiming
    /// UPDATE, regardless of which process either is running in.</param>
    Task AdvanceAsync(
        AiPipelineRun run, TenantMemberRole role, CancellationToken cancellationToken, string leaseOwner = "orchestrator");

    /// <summary>Marks a run <see cref="AiPipelineRunStatus.Cancelled"/>. In-flight stage rows are left
    /// as they are — a cancelled run simply stops being advanced, rather than requiring every stage to
    /// be rewritten to a new terminal state it didn't actually reach.</summary>
    Task CancelAsync(AiPipelineRun run, CancellationToken cancellationToken);

    /// <summary>Prepares a run's <c>ContentPlan</c> stage for one more batch of posts, then saves —
    /// the caller still has to call <see cref="AdvanceAsync"/> separately to actually run it. Creates
    /// the stage row if this run predates content generation entirely (every run a pre-C19 campaign's
    /// approval was backfilled onto never got one), or resets it from <c>Completed</c>/<c>Failed</c>
    /// back to <c>Pending</c> with <c>CoinsCharged</c> zeroed — unlike every other stage, each
    /// <c>ContentPlan</c> batch is its own billable action, not a one-time charge to guard against
    /// repeating. A <c>Running</c> stage is left alone: something else is already generating a batch
    /// for this run.</summary>
    Task<AiPipelineStage> EnsureContentBatchAsync(AiPipelineRun run, CancellationToken cancellationToken);
}

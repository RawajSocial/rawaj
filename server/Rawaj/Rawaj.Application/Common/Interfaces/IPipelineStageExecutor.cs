using Rawaj.Domain.Entities.AiOperations;
using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Common.Interfaces;

/// <summary>
/// Everything a stage needs to do its work, assembled by the orchestrator before dispatch.
///
/// <para>Note what an executor is <i>not</i> given: it does not look up its own inputs, decide
/// whether it may run, charge for itself, or choose what happens next. Dependencies arrive already
/// resolved in <see cref="Inputs"/>, ordering comes from <c>AiPipelinePolicy</c>, and charging from
/// <c>AiPipelineCoinPolicy</c>. An executor turns inputs into one artifact and says whether that
/// worked — which is what makes each of them small enough to test against a substituted provider.</para>
/// </summary>
/// <param name="Inputs">The current payloads of this stage's dependency artifacts, keyed by kind.
/// An optional dependency that was skipped is simply absent, so executors must treat every entry as
/// possibly missing rather than assuming the full set.</param>
/// <param name="RepairPrompt">True when the previous attempt produced unusable JSON and this one
/// should carry a repair instruction. Decided by <c>AiPipelinePolicy.ShouldRepairPrompt</c>.</param>
public sealed record StageContext(
    AiPipelineRun Run,
    AiPipelineStage Stage,
    TenantBrandProfile Brand,
    MarketingCampaign? Campaign,
    IReadOnlyDictionary<AiArtifactKind, string> Inputs,
    Guid UserId,
    TenantMemberRole Role,
    bool RepairPrompt)
{
    public Guid TenantId => Run.TenantId;

    public string? Input(AiArtifactKind kind) => Inputs.TryGetValue(kind, out var json) ? json : null;
}

/// <summary>
/// What a stage produced, and what the orchestrator should do about it.
/// </summary>
/// <param name="ArtifactJson">The validated payload to store. Null for stages that produce no
/// artifact of their own (human approval, the image stages, which write a VisualAsset instead).</param>
/// <param name="ValueDelivered">Whether anything worth charging for came out of this. Competitor
/// research sets it false on an empty result: the stage completed, the flow continues, and the
/// tenant is not billed — exactly today's behaviour.</param>
/// <param name="FanOutTargets">Entity ids for which the orchestrator should create follow-on stage
/// rows. Only <c>ContentPlan</c> uses this today, returning one ContentItem id per generated post so
/// each image becomes its own retryable stage.</param>
/// <param name="AiJobIds">Provider-call log rows this stage created, so the orchestrator can point
/// them at the stage. Plural because one stage can make several calls — a research stage searches
/// and then synthesises.</param>
/// <param name="ReusedArtifactId">Set when the stage satisfied itself from an existing artifact
/// instead of generating one. The orchestrator points the stage at it and charges nothing: no
/// provider was called, so there is nothing to bill for. Distinct from producing an artifact, which
/// would otherwise write a needless duplicate version of identical content.</param>
/// <param name="InputHash">The fingerprint of what this execution was based on, recorded on the
/// stage so a later re-invocation can tell whether anything has actually changed.</param>
public sealed record StageResult(
    bool Succeeded,
    string? ArtifactJson = null,
    AiArtifactKind? ArtifactKind = null,
    bool ValueDelivered = true,
    AiFailureKind? FailureKind = null,
    string? ErrorMessage = null,
    IReadOnlyList<Guid>? FanOutTargets = null,
    IReadOnlyList<Guid>? AiJobIds = null,
    Guid? ReusedArtifactId = null,
    string? InputHash = null)
{
    public static StageResult Success(AiArtifactKind kind, string artifactJson, IReadOnlyList<Guid>? aiJobIds = null) =>
        new(true, artifactJson, kind, AiJobIds: aiJobIds);

    /// <summary>Completed, but produced nothing billable — the research "found nothing" case.</summary>
    public static StageResult SucceededWithoutValue(
        AiArtifactKind kind, string artifactJson, IReadOnlyList<Guid>? aiJobIds = null) =>
        new(true, artifactJson, kind, ValueDelivered: false, AiJobIds: aiJobIds);

    /// <summary>Completed and produced no artifact — approval, and the image stages.</summary>
    public static StageResult Completed(IReadOnlyList<Guid>? aiJobIds = null) =>
        new(true, AiJobIds: aiJobIds);

    /// <summary>Satisfied from an existing artifact — no provider call, nothing to charge.</summary>
    public static StageResult Reused(Guid artifactId) =>
        new(true, ReusedArtifactId: artifactId, ValueDelivered: false);

    public static StageResult FannedOut(
        AiArtifactKind kind, string artifactJson, IReadOnlyList<Guid> targets, IReadOnlyList<Guid>? aiJobIds = null) =>
        new(true, artifactJson, kind, FanOutTargets: targets, AiJobIds: aiJobIds);

    public static StageResult Failure(AiFailureKind failureKind, string errorMessage, IReadOnlyList<Guid>? aiJobIds = null) =>
        new(false, FailureKind: failureKind, ErrorMessage: errorMessage, AiJobIds: aiJobIds);
}

/// <summary>
/// One stage's work. Implementations are registered by <see cref="Kind"/> and resolved by the
/// orchestrator, so adding a stage means adding one class and one graph entry — not editing a
/// switch somewhere.
///
/// <para>Implementations must not call the next stage, mark themselves complete, or charge. They
/// execute, return, and stop. That is what allows the same executor to serve a full run, a
/// single-stage invocation and a resume with no branching between the three.</para>
/// </summary>
public interface IPipelineStageExecutor
{
    AiPipelineStageKind Kind { get; }

    Task<StageResult> ExecuteAsync(StageContext context, CancellationToken cancellationToken);
}

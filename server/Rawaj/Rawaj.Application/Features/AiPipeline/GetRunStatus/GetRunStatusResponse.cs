using Rawaj.Application.Common.Policies;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.AiPipeline.GetRunStatus;

/// <param name="TargetRefId">For a fanned-out stage (today, only <c>ContentImage</c>), the id of the
/// entity it's producing — a ContentItem's id. Null for every other stage kind. Lets a client tell
/// which posts belong to *this* run's batch apart from ones a campaign already had from an earlier
/// generation, since ContentItem itself carries no run/batch reference of its own.</param>
public record StageStatusSummary(
    AiPipelineStageKind Kind, AiPipelineStageStatus Status, int Attempts, int MaxAttempts, string? LastError,
    Guid? TargetRefId);

/// <param name="Version">Monotonic counter — see <c>AiPipelineRun.Version</c> remarks. Lets the
/// client discard a poll response that turns out to be stale relative to a SignalR push it already
/// applied (or vice versa), rather than trusting whichever one happens to arrive last.</param>
public record GetRunStatusResponse(
    Guid RunId,
    AiPipelineRunStatus Status,
    PipelineProgress Progress,
    int TotalCoinsSpent,
    string? LastError,
    IReadOnlyList<StageStatusSummary> Stages,
    int Version);

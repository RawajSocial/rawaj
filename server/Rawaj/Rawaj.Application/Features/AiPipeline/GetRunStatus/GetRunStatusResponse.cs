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

public record GetRunStatusResponse(
    Guid RunId,
    AiPipelineRunStatus Status,
    PipelineProgress Progress,
    int TotalCoinsSpent,
    string? LastError,
    IReadOnlyList<StageStatusSummary> Stages);

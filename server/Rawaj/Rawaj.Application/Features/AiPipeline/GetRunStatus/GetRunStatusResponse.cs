using Rawaj.Application.Common.Policies;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.AiPipeline.GetRunStatus;

public record StageStatusSummary(
    AiPipelineStageKind Kind, AiPipelineStageStatus Status, int Attempts, int MaxAttempts, string? LastError);

public record GetRunStatusResponse(
    Guid RunId,
    AiPipelineRunStatus Status,
    PipelineProgress Progress,
    int TotalCoinsSpent,
    string? LastError,
    IReadOnlyList<StageStatusSummary> Stages);

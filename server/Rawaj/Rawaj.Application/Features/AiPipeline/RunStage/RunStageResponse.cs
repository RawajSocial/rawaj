using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.AiPipeline.RunStage;

public record RunStageResponse(Guid RunId, AiPipelineRunStatus RunStatus, AiPipelineStageKind Kind, AiPipelineStageStatus StageStatus);

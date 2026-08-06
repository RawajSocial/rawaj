using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.AiPipeline.StartRun;

public record StartRunResponse(Guid RunId, AiPipelineRunStatus Status);

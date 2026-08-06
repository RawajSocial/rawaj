using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.AiPipeline.CancelRun;

public record CancelRunResponse(Guid RunId, AiPipelineRunStatus Status);

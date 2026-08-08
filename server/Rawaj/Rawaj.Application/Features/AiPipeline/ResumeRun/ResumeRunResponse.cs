using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.AiPipeline.ResumeRun;

public record ResumeRunResponse(Guid RunId, AiPipelineRunStatus Status);

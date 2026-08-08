namespace Rawaj.Application.Features.AiPipeline.ApproveStrategy;

public record ApproveStrategyResponse(Guid RunId, Guid ApprovedArtifactId, DateTime ApprovedAt);

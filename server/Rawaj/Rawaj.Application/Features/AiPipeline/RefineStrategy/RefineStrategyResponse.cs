namespace Rawaj.Application.Features.AiPipeline.RefineStrategy;

public record RefineStrategyResponse(Guid ArtifactId, int Version, string ContentJson);

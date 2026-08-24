using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.AiPipeline.GetArtifact;

public record GetArtifactResponse(AiArtifactKind Kind, int Version, string ContentJson, DateTime CreatedAt);

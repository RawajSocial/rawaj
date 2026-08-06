using Rawaj.Domain.Entities.AiOperations;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Common.Interfaces;

/// <summary>
/// Reads and writes versioned stage outputs.
///
/// <para>Versioning is the reason this is a store and not a column write. Today
/// <c>RefineCampaignPlan</c> overwrites <c>AiPlanJson</c> in place, so a user who refines twice
/// cannot go back to the version they preferred, and an approval recorded as a bare timestamp cannot
/// say which strategy it approved. Here a refinement writes v2 and v1 remains.</para>
/// </summary>
public interface IPipelineArtifactStore
{
    /// <summary>The current version of one artifact kind. Brand-scoped kinds resolve by brand;
    /// everything else by campaign.</summary>
    Task<AiArtifact?> GetCurrentAsync(
        Guid brandProfileId, Guid? campaignId, AiArtifactKind kind, CancellationToken cancellationToken);

    /// <summary>The payloads of several kinds at once, for assembling a stage's inputs in one query.
    /// Kinds with no current artifact are absent from the result rather than present and null — an
    /// optional stage that was skipped genuinely has nothing to contribute.</summary>
    Task<IReadOnlyDictionary<AiArtifactKind, string>> GetCurrentPayloadsAsync(
        Guid brandProfileId, Guid? campaignId, IReadOnlyCollection<AiArtifactKind> kinds, CancellationToken cancellationToken);

    Task<AiArtifact?> GetVersionAsync(
        Guid brandProfileId, Guid? campaignId, AiArtifactKind kind, int version, CancellationToken cancellationToken);

    /// <summary>
    /// A reusable artifact whose inputs still hash to <paramref name="inputHash"/> — the brand
    /// analysis cache. A miss means the brand changed since it was computed, and the next campaign
    /// pays to refresh it.
    /// </summary>
    Task<AiArtifact?> FindByInputHashAsync(
        Guid brandProfileId, AiArtifactKind kind, string inputHash, CancellationToken cancellationToken);

    /// <summary>
    /// Stores a new version and makes it current, demoting the previous one. Does not save — the
    /// caller commits, so an artifact and the stage row that produced it land in the same
    /// transaction and a crash between them is impossible.
    /// </summary>
    /// <param name="stage">The stage that produced this version, so the artifact and the stage that
    /// made it can be traced to each other. Null for a write with no backing stage — strategy
    /// refinement is a versioned write too (the whole point is that it must not overwrite the
    /// artifact in place), but the graph has no node for "user typed feedback into a box".</param>
    Task<AiArtifact> AddVersionAsync(
        AiPipelineStage? stage,
        Guid tenantId,
        Guid brandProfileId,
        Guid? campaignId,
        AiArtifactKind kind,
        string contentJson,
        string? inputHash,
        CancellationToken cancellationToken);
}

using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Domain.Entities.AiOperations;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Common.Services;

/// <inheritdoc cref="IPipelineArtifactStore"/>
public class PipelineArtifactStore(IApplicationDbContext dbContext) : IPipelineArtifactStore
{
    /// <summary>
    /// Kinds that belong to the brand rather than to a campaign, and so are looked up and reused
    /// across every campaign of that brand.
    /// </summary>
    private static bool IsBrandScoped(AiArtifactKind kind) => kind == AiArtifactKind.BrandAnalysis;

    private const int CurrentSchemaVersion = 1;

    public async Task<AiArtifact?> GetCurrentAsync(
        Guid brandProfileId, Guid? campaignId, AiArtifactKind kind, CancellationToken cancellationToken) =>
        await Scope(brandProfileId, campaignId, kind)
            .Where(a => a.IsCurrent)
            .OrderByDescending(a => a.Version)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<AiArtifactKind, string>> GetCurrentPayloadsAsync(
        Guid brandProfileId, Guid? campaignId, IReadOnlyCollection<AiArtifactKind> kinds, CancellationToken cancellationToken)
    {
        if (kinds.Count == 0)
        {
            return new Dictionary<AiArtifactKind, string>();
        }

        var brandScoped = kinds.Where(IsBrandScoped).ToList();
        var campaignScoped = kinds.Where(k => !IsBrandScoped(k)).ToList();

        var results = new Dictionary<AiArtifactKind, string>();

        if (campaignScoped.Count > 0)
        {
            var rows = await dbContext.AiArtifacts
                .Where(a => a.CampaignId == campaignId && campaignScoped.Contains(a.Kind) && a.IsCurrent)
                .Select(a => new { a.Kind, a.ContentJson })
                .ToListAsync(cancellationToken);

            foreach (var row in rows)
            {
                results[row.Kind] = row.ContentJson;
            }
        }

        if (brandScoped.Count > 0)
        {
            var rows = await dbContext.AiArtifacts
                .Where(a => a.BrandProfileId == brandProfileId && brandScoped.Contains(a.Kind) && a.IsCurrent)
                .Select(a => new { a.Kind, a.ContentJson })
                .ToListAsync(cancellationToken);

            foreach (var row in rows)
            {
                results[row.Kind] = row.ContentJson;
            }
        }

        return results;
    }

    public async Task<AiArtifact?> GetVersionAsync(
        Guid brandProfileId, Guid? campaignId, AiArtifactKind kind, int version, CancellationToken cancellationToken) =>
        await Scope(brandProfileId, campaignId, kind)
            .FirstOrDefaultAsync(a => a.Version == version, cancellationToken);

    public async Task<AiArtifact?> FindByInputHashAsync(
        Guid brandProfileId, AiArtifactKind kind, string inputHash, CancellationToken cancellationToken) =>
        await dbContext.AiArtifacts
            .Where(a => a.BrandProfileId == brandProfileId && a.Kind == kind && a.InputHash == inputHash)
            .OrderByDescending(a => a.Version)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<AiArtifact> AddVersionAsync(
        AiPipelineStage? stage,
        Guid tenantId,
        Guid brandProfileId,
        Guid? campaignId,
        AiArtifactKind kind,
        string contentJson,
        string? inputHash,
        CancellationToken cancellationToken)
    {
        // Demote every existing current row rather than only the highest-versioned one: if a bug or
        // an interrupted write ever left two marked current, this heals it instead of compounding it.
        var existing = await Scope(brandProfileId, campaignId, kind)
            .Where(a => a.IsCurrent)
            .ToListAsync(cancellationToken);

        foreach (var artifact in existing)
        {
            artifact.IsCurrent = false;
        }

        var highestVersion = await Scope(brandProfileId, campaignId, kind)
            .Select(a => (int?)a.Version)
            .MaxAsync(cancellationToken) ?? 0;

        var created = new AiArtifact
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BrandProfileId = brandProfileId,
            CampaignId = IsBrandScoped(kind) ? null : campaignId,
            Kind = kind,
            Version = highestVersion + 1,
            IsCurrent = true,
            ContentJson = contentJson,
            SchemaVersion = CurrentSchemaVersion,
            SourceStageId = stage?.Id,
            InputHash = inputHash,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.AiArtifacts.Add(created);

        if (stage is not null)
        {
            stage.ArtifactId = created.Id;
        }

        return created;
    }

    private IQueryable<AiArtifact> Scope(Guid brandProfileId, Guid? campaignId, AiArtifactKind kind) =>
        IsBrandScoped(kind)
            ? dbContext.AiArtifacts.Where(a => a.BrandProfileId == brandProfileId && a.Kind == kind)
            : dbContext.AiArtifacts.Where(a => a.CampaignId == campaignId && a.Kind == kind);
}

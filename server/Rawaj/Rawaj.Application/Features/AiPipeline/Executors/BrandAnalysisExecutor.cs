using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Services;
using Rawaj.Application.Features.AiPipeline.Common;
using Rawaj.Application.Features.AiPipeline.Prompts;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.AiPipeline.Executors;

/// <summary>
/// Produces the brand's durable analysis — or reuses the existing one.
///
/// <para>The cache is the substance of this stage. A brand's analysis is the same answer for every
/// campaign it runs until its profile changes, so the second campaign and every campaign after it
/// should get it instantly and for free. The key is a hash of the identity fields the prompt
/// actually reads, which means an edit that changes nothing the analysis depends on does not
/// invalidate it.</para>
///
/// <para>A stale hash does <b>not</b> trigger a recomputation on the spot: that would spend the
/// tenant's coins with no user action to attribute the spend to. It simply misses, and the campaign
/// currently running pays to refresh it.</para>
/// </summary>
public class BrandAnalysisExecutor(
    IApplicationDbContext dbContext,
    IAiTextGenerationService textGenerationService,
    IPromptTemplateProvider templates,
    IPipelineArtifactStore artifacts)
    : TextStageExecutor(dbContext, textGenerationService, templates)
{
    public override AiPipelineStageKind Kind => AiPipelineStageKind.BrandAnalysis;

    protected override AiArtifactKind ArtifactKind => AiArtifactKind.BrandAnalysis;

    protected override string? BuildPrompt(StageContext context) =>
        BrandAnalysisPrompt.Build(context.Brand, context.Campaign?.BriefJson);

    public override async Task<StageResult> ExecuteAsync(StageContext context, CancellationToken cancellationToken)
    {
        var inputHash = ComputeInputHash(context.Brand);
        context.Stage.InputHash = inputHash;

        var cached = await artifacts.FindByInputHashAsync(
            context.Brand.Id, AiArtifactKind.BrandAnalysis, inputHash, cancellationToken);

        if (cached is not null)
        {
            context.Brand.CurrentBrandAnalysisArtifactId = cached.Id;
            return StageResult.Reused(cached.Id);
        }

        var result = await base.ExecuteAsync(context, cancellationToken);

        return result with { InputHash = inputHash };
    }

    /// <summary>
    /// Hashes only what the prompt reads. Renaming the brand or editing a field the analysis never
    /// saw should not throw away a perfectly good artifact and charge for another one.
    /// </summary>
    private static string ComputeInputHash(TenantBrandProfile brand) =>
        PipelineInputHash.Compute(
            brand.Name,
            brand.Description,
            brand.BrandVoice?.ToString(),
            brand.BrandInfo?.Tagline,
            brand.BrandInfo?.Industry,
            brand.BrandInfo?.TargetAudience,
            brand.BrandInfo?.Keywords is { Count: > 0 } keywords ? string.Join(",", keywords) : null);
}

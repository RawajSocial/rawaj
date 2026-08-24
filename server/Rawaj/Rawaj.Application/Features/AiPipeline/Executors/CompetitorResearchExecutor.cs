using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.AiPipeline.Common;
using Rawaj.Application.Features.AiPipeline.Prompts;
using Rawaj.Domain.Entities.BrandIntelligence;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.AiPipeline.Executors;

/// <summary>
/// "What are the businesses competing with this one doing" — who they are, how they position, what
/// content patterns they follow, and what none of them cover.
///
/// <para>Carries the <c>CompetitiveAnalysis</c> charge, and only when it actually found something:
/// an empty or failed search completes with <c>unavailable: true</c> and bills nothing, preserving
/// today's rule exactly.</para>
/// </summary>
public class CompetitorResearchExecutor(
    IApplicationDbContext dbContext,
    ITavilySearchService searchService,
    IAiTextGenerationService textGenerationService,
    IPromptTemplateProvider templates)
    : ResearchStageExecutor(dbContext, searchService, textGenerationService, templates)
{
    private readonly IApplicationDbContext _dbContext = dbContext;

    public override AiPipelineStageKind Kind => AiPipelineStageKind.CompetitorResearch;

    protected override AiArtifactKind ArtifactKind => AiArtifactKind.CompetitorResearch;

    protected override string QuerySelector => "competitor";

    protected override string BuildSynthesisPrompt(
        TenantBrandProfile brand, IReadOnlyList<string> queries, IReadOnlyList<string> excerpts, string? searchAnswer) =>
        ResearchSynthesisPrompt.BuildCompetitor(brand, queries, excerpts, searchAnswer);

    protected override string BuildFallbackQuery(TenantBrandProfile brand)
    {
        var parts = new List<string> { $"{brand.Name} main competitors and market landscape" };

        if (!string.IsNullOrWhiteSpace(brand.BrandInfo?.Industry))
        {
            parts.Add($"in the {brand.BrandInfo.Industry} industry");
        }

        if (brand.BrandInfo?.Keywords is { Count: > 0 })
        {
            parts.Add($"related to {string.Join(", ", brand.BrandInfo.Keywords)}");
        }

        if (!string.IsNullOrWhiteSpace(brand.BrandInfo?.Location))
        {
            parts.Add($"in {brand.BrandInfo.Location}");
        }

        return string.Join(" ", parts);
    }

    /// <summary>
    /// Keeps the raw pages as RagDocument rows, as the current handler does. Brand-level readers
    /// consume these independently of any campaign, so dropping them here would quietly remove
    /// competitor context from the standalone content generators.
    /// </summary>
    protected override void OnResultsGathered(StageContext context, IReadOnlyList<TavilySearchItem> items)
    {
        var now = DateTime.UtcNow;

        foreach (var item in items)
        {
            _dbContext.RagDocuments.Add(new RagDocument
            {
                Id = Guid.NewGuid(),
                BrandProfileId = context.Brand.Id,
                SourceType = RagSourceType.Website,
                SourceUrl = item.Url,
                CompetitorsData = item.Content,
                IndexedAt = now,
                CreatedAt = now
            });
        }
    }
}

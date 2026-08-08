using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Features.AiPipeline.Common;
using Rawaj.Application.Features.AiPipeline.Prompts;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.AiPipeline.Executors;

/// <summary>
/// "What is happening in this market" — trends, demand, seasonality, platform norms.
///
/// <para>New capability. Today a single competitor search doubles as market context, which answers
/// neither question well: the queries that surface competing businesses are not the queries that
/// surface demand signals or seasonality. Splitting them means each gets its own searches, its own
/// synthesis prompt, and its own failure — one can come back empty without taking the other with
/// it.</para>
///
/// <para>Free: folded into the existing research charge point rather than priced as a new line item,
/// so a full run still costs what it costs today.</para>
/// </summary>
public class MarketResearchExecutor(
    IApplicationDbContext dbContext,
    ITavilySearchService searchService,
    IAiTextGenerationService textGenerationService,
    IPromptTemplateProvider templates)
    : ResearchStageExecutor(dbContext, searchService, textGenerationService, templates)
{
    public override AiPipelineStageKind Kind => AiPipelineStageKind.MarketResearch;

    protected override AiArtifactKind ArtifactKind => AiArtifactKind.MarketResearch;

    protected override string QuerySelector => "market";

    protected override string BuildSynthesisPrompt(
        TenantBrandProfile brand, IReadOnlyList<string> queries, IReadOnlyList<string> excerpts, string? searchAnswer) =>
        ResearchSynthesisPrompt.BuildMarket(brand, queries, excerpts, searchAnswer);

    protected override string BuildFallbackQuery(TenantBrandProfile brand)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(brand.BrandInfo?.Industry))
        {
            parts.Add($"{brand.BrandInfo.Industry} market trends and customer demand");
        }
        else
        {
            parts.Add($"{brand.Name} market trends and customer demand");
        }

        if (!string.IsNullOrWhiteSpace(brand.BrandInfo?.Location))
        {
            parts.Add($"in {brand.BrandInfo.Location}");
        }

        return string.Join(" ", parts);
    }
}

using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Features.AiPipeline.Common;
using Rawaj.Application.Features.AiPipeline.Prompts;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.AiPipeline.Executors;

/// <summary>
/// The business-and-market analysis, brand strategy and marketing strategy — the interpretive third
/// of the strategy, grounded in the fullest set of inputs the graph has by this point (campaign
/// analysis plus whatever research came back).
/// </summary>
public class StrategyPositioningExecutor(
    IApplicationDbContext dbContext,
    IAiTextGenerationService textGenerationService,
    IPromptTemplateProvider templates)
    : TextStageExecutor(dbContext, textGenerationService, templates)
{
    public override AiPipelineStageKind Kind => AiPipelineStageKind.StrategyPositioning;

    protected override AiArtifactKind ArtifactKind => AiArtifactKind.StrategyPositioning;

    protected override string? BuildPrompt(StageContext context)
    {
        // A positioning with no campaign is meaningless. Unreachable via the graph, but the contract
        // is the same as every other campaign-scoped stage: say so rather than prompting with holes.
        if (context.Campaign is null)
        {
            return null;
        }

        return StrategyPrompt.BuildPositioning(
            context.Brand,
            context.Campaign,
            context.Input(AiArtifactKind.CampaignAnalysis),
            context.Input(AiArtifactKind.MarketResearch),
            context.Input(AiArtifactKind.CompetitorResearch));
    }
}

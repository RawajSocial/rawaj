using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Features.AiPipeline.Common;
using Rawaj.Application.Features.AiPipeline.Prompts;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.AiPipeline.Executors;

/// <summary>
/// The execution roadmap, content production plan, AI recommendations, and the executive summary
/// that opens the assembled strategy. Builds on the blueprint rather than duplicating it, which is
/// why the graph makes this stage wait for StrategyBlueprint specifically rather than running it
/// alongside the other two.
/// </summary>
public class StrategyRoadmapExecutor(
    IApplicationDbContext dbContext,
    IAiTextGenerationService textGenerationService,
    IPromptTemplateProvider templates)
    : TextStageExecutor(dbContext, textGenerationService, templates)
{
    public override AiPipelineStageKind Kind => AiPipelineStageKind.StrategyRoadmap;

    protected override AiArtifactKind ArtifactKind => AiArtifactKind.StrategyRoadmap;

    protected override string? BuildPrompt(StageContext context)
    {
        if (context.Campaign is null)
        {
            return null;
        }

        return StrategyPrompt.BuildRoadmap(
            context.Brand,
            context.Campaign,
            context.Input(AiArtifactKind.CampaignAnalysis),
            context.Input(AiArtifactKind.MarketResearch),
            context.Input(AiArtifactKind.StrategyBlueprint));
    }
}

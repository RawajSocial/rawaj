using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Features.AiPipeline.Common;
using Rawaj.Application.Features.AiPipeline.Prompts;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.AiPipeline.Executors;

/// <summary>
/// The campaign blueprint — pillars, key themes, posting cadence, content mix, recommended platforms.
/// Deliberately does not depend on market research: cadence and platform mix follow from the
/// campaign's own constraints and its competitive landscape, not from demand trends.
/// </summary>
public class StrategyBlueprintExecutor(
    IApplicationDbContext dbContext,
    IAiTextGenerationService textGenerationService,
    IPromptTemplateProvider templates)
    : TextStageExecutor(dbContext, textGenerationService, templates)
{
    public override AiPipelineStageKind Kind => AiPipelineStageKind.StrategyBlueprint;

    protected override AiArtifactKind ArtifactKind => AiArtifactKind.StrategyBlueprint;

    protected override string? BuildPrompt(StageContext context)
    {
        if (context.Campaign is null)
        {
            return null;
        }

        return StrategyPrompt.BuildBlueprint(
            context.Brand,
            context.Campaign,
            context.Input(AiArtifactKind.CampaignAnalysis),
            context.Input(AiArtifactKind.CompetitorResearch));
    }
}

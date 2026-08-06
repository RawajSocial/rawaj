using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Features.AiPipeline.Common;
using Rawaj.Application.Features.AiPipeline.Prompts;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.AiPipeline.Executors;

/// <summary>
/// Reads the campaign: what it is actually trying to achieve, for whom, under what constraints — and
/// what should be researched before a strategy is written.
///
/// <para>Carries the business-understanding charge for itself and for brand analysis together, which
/// is what keeps the split from costing the tenant more than the single diagnosis it replaces (see
/// <c>AiPipelineCoinPolicy</c>). Charging on this stage rather than on brand analysis also avoids
/// pricing the same work differently run to run, since brand analysis is frequently a cache hit.</para>
/// </summary>
public class CampaignAnalysisExecutor(
    IApplicationDbContext dbContext,
    IAiTextGenerationService textGenerationService,
    IPromptTemplateProvider templates)
    : TextStageExecutor(dbContext, textGenerationService, templates)
{
    public override AiPipelineStageKind Kind => AiPipelineStageKind.CampaignAnalysis;

    protected override AiArtifactKind ArtifactKind => AiArtifactKind.CampaignAnalysis;

    protected override string? BuildPrompt(StageContext context)
    {
        // A campaign analysis with no campaign is meaningless. The graph makes this unreachable for
        // a campaign run, but a brand-only run would otherwise prompt with holes in it.
        if (context.Campaign is null)
        {
            return null;
        }

        return CampaignAnalysisPrompt.Build(
            context.Brand,
            context.Campaign,
            context.Campaign.BriefJson,
            context.Input(AiArtifactKind.BrandAnalysis));
    }
}

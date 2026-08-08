using Rawaj.Application.Common.Interfaces;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Common.Services;

/// <summary>
/// Default per-stage generation settings.
///
/// <para>Temperatures are chosen by what a wrong answer costs. Analysis and assembly are structural:
/// the same inputs should give the same reading, so they run cold. Copywriting is the opposite — a
/// batch of fifteen posts that all sound identical is a defect, so it runs warm. Research synthesis
/// sits between: summarise faithfully, but don't parrot.</para>
///
/// <para>Task names are config keys, so a later change can point an expensive stage at a better
/// model without touching any executor.</para>
/// </summary>
public class PromptTemplateProvider : IPromptTemplateProvider
{
    public PromptTemplate For(AiPipelineStageKind kind) => kind switch
    {
        AiPipelineStageKind.BrandAnalysis => new("BrandAnalysis", JsonMode: true, Temperature: 0.2),
        AiPipelineStageKind.CampaignAnalysis => new("CampaignAnalysis", JsonMode: true, Temperature: 0.2),

        AiPipelineStageKind.MarketResearch => new("Research", JsonMode: true, Temperature: 0.3),
        AiPipelineStageKind.CompetitorResearch => new("Research", JsonMode: true, Temperature: 0.3),

        // The most valuable output in the product. Warm enough to produce a strategy with a point of
        // view rather than a restatement of the inputs, cold enough to stay grounded in them.
        AiPipelineStageKind.StrategyPositioning => new("Strategy", JsonMode: true, Temperature: 0.5),
        AiPipelineStageKind.StrategyBlueprint => new("Strategy", JsonMode: true, Temperature: 0.5),
        AiPipelineStageKind.StrategyRoadmap => new("Strategy", JsonMode: true, Temperature: 0.5),

        // Pure composition, no model call — settings are inert but returning a template keeps every
        // stage answerable rather than making callers special-case this one.
        AiPipelineStageKind.StrategyAssemble => new("Assemble", JsonMode: true, Temperature: 0),

        AiPipelineStageKind.ContentPlan => new("Content", JsonMode: true, Temperature: 0.8),

        // No text model involved; the image provider takes the prompt as-is.
        AiPipelineStageKind.ContentImage => new("Image", JsonMode: false, Temperature: null),

        _ => new(kind.ToString(), JsonMode: false, Temperature: null)
    };
}

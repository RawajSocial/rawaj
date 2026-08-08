using System.Text.Json;
using System.Text.Json.Nodes;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Features.AiPipeline.Common;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.AiPipeline.Executors;

/// <summary>
/// Assembles the three strategy sub-artifacts into the single, user-facing Strategy artifact.
///
/// <para>Pure composition and schema validation — no model call, hence no AiJob and nothing of its
/// own to charge for; the 12,000-coin marketing-plan charge lands on this stage precisely because it
/// is the point where all three sub-strategies exist and the tenant can be billed once for the whole
/// thing rather than three times for its parts.</para>
///
/// <para>The output shape has to stay byte-compatible with what <c>MarketingCampaign.AiPlanJson</c>
/// holds today: the strategy review UI, campaign-strategy-page and the content prompt all read it
/// directly.</para>
/// </summary>
public class StrategyAssembleExecutor : IPipelineStageExecutor
{
    public AiPipelineStageKind Kind => AiPipelineStageKind.StrategyAssemble;

    public Task<StageResult> ExecuteAsync(StageContext context, CancellationToken cancellationToken)
    {
        var positioning = context.Input(AiArtifactKind.StrategyPositioning);
        var blueprint = context.Input(AiArtifactKind.StrategyBlueprint);
        var roadmap = context.Input(AiArtifactKind.StrategyRoadmap);

        // Unreachable via the graph — none of the three are optional, so reaching this stage implies
        // all three completed — but a stage that cannot do its job should say so rather than assemble
        // a strategy with silent holes in it.
        if (positioning is null || blueprint is null || roadmap is null)
        {
            return Task.FromResult(StageResult.Failure(
                AiFailureKind.Validation, "The strategy cannot be assembled without all three of its parts."));
        }

        string assembled;
        try
        {
            assembled = Assemble(positioning, blueprint, roadmap);
        }
        catch (JsonException ex)
        {
            return Task.FromResult(StageResult.Failure(
                AiFailureKind.Parse, $"A strategy sub-artifact was not valid JSON: {ex.Message}"));
        }

        if (!ArtifactSchema.IsValid(AiArtifactKind.Strategy, assembled, out var validationError))
        {
            return Task.FromResult(StageResult.Failure(
                AiFailureKind.Validation, validationError ?? "The assembled strategy was missing required fields."));
        }

        return Task.FromResult(StageResult.Success(AiArtifactKind.Strategy, assembled));
    }

    private static string Assemble(string positioningJson, string blueprintJson, string roadmapJson)
    {
        var positioning = JsonNode.Parse(positioningJson)!.AsObject();
        var blueprint = JsonNode.Parse(blueprintJson)!.AsObject();
        var roadmap = JsonNode.Parse(roadmapJson)!.AsObject();

        var strategy = new JsonObject();

        CopyIfPresent(roadmap, strategy, "executiveSummary");
        CopyIfPresent(positioning, strategy, "businessAndMarketAnalysis");
        CopyIfPresent(positioning, strategy, "brandStrategy");
        CopyIfPresent(positioning, strategy, "marketingStrategy");
        CopyIfPresent(blueprint, strategy, "campaignBlueprint");
        CopyIfPresent(roadmap, strategy, "contentProductionPlan");
        CopyIfPresent(roadmap, strategy, "executionRoadmap");
        CopyIfPresent(roadmap, strategy, "aiRecommendations");

        return strategy.ToJsonString();
    }

    private static void CopyIfPresent(JsonObject source, JsonObject target, string property)
    {
        if (source.TryGetPropertyValue(property, out var value) && value is not null)
        {
            target[property] = value.DeepClone();
        }
    }
}

using System.Text.Json;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.AiPipeline.Common;

/// <summary>
/// Validates a stage's output before it is stored.
///
/// <para>An artifact that does not validate is not stored and its stage fails. That rule exists
/// because of a real incident shape: unreadable text written into a column named for a strategy
/// passes every "does a plan exist" check in the codebase while rendering as a blank page to the
/// user who paid 12,000 coins for it. Failing loudly at the moment of production is cheap; a
/// silently empty artifact is discovered by a customer.</para>
///
/// <para>Checks are structural — the required top-level keys are present and the payload is an
/// object — not semantic. Judging whether a strategy is *good* is not something a schema can do, and
/// pretending otherwise would only produce false confidence.</para>
/// </summary>
public static class ArtifactSchema
{
    private static readonly Dictionary<AiArtifactKind, string[]> RequiredProperties = new()
    {
        [AiArtifactKind.BrandAnalysis] =
            ["businessSummary", "identityCore", "voiceProfile", "valuePropositions", "differentiators"],

        [AiArtifactKind.CampaignAnalysis] =
            ["objectiveClassification", "audienceSegments", "successCriteria", "researchQueries"],

        [AiArtifactKind.MarketResearch] = ["trends", "sources"],
        [AiArtifactKind.CompetitorResearch] = ["competitors", "sources"],

        [AiArtifactKind.StrategyPositioning] =
            ["businessAndMarketAnalysis", "brandStrategy", "marketingStrategy"],
        [AiArtifactKind.StrategyBlueprint] = ["campaignBlueprint"],
        [AiArtifactKind.StrategyRoadmap] =
            ["contentProductionPlan", "executionRoadmap", "aiRecommendations", "executiveSummary"],

        // The assembled, user-facing strategy. These keys are exactly what the 7-section review UI,
        // campaign-strategy-page and the content prompt read out of AiPlanJson today, so the shape
        // has to stay byte-compatible with what those already expect.
        [AiArtifactKind.Strategy] =
        [
            "executiveSummary", "businessAndMarketAnalysis", "brandStrategy", "marketingStrategy",
            "campaignBlueprint", "contentProductionPlan", "executionRoadmap", "aiRecommendations"
        ],

        [AiArtifactKind.ContentPlan] = ["posts"]
    };

    /// <param name="error">Written for the stage's LastError, so a validation failure says which key
    /// was missing rather than "invalid output".</param>
    public static bool IsValid(AiArtifactKind kind, string? json, out string? error)
    {
        error = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            error = "The model returned an empty response.";
            return false;
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            error = $"The model's response was not valid JSON: {ex.Message}";
            return false;
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                error = "The model's response was not a JSON object.";
                return false;
            }

            if (!RequiredProperties.TryGetValue(kind, out var required))
            {
                return true;
            }

            var missing = required
                .Where(property => !document.RootElement.TryGetProperty(property, out _))
                .ToList();

            if (missing.Count > 0)
            {
                error = $"The model's response was missing required field(s): {string.Join(", ", missing)}.";
                return false;
            }
        }

        return true;
    }
}

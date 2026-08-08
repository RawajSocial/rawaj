using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.Entities.Tenants;

namespace Rawaj.Application.Features.AiPipeline.Prompts;

/// <summary>
/// The strategy stages' prompts. Deliberately three prompts instead of one: the single "write the
/// whole strategy" prompt it replaces asks one call to hold the campaign's objective, brand voice,
/// market data and competitor data, and produce eight structurally different sections at once — so
/// any one section going wrong, or the response getting truncated, discards the entire 12,000-coin
/// artifact. Splitting it means a bad blueprint retries only the blueprint.
///
/// <para>All three read the synthesised CampaignAnalysis and research artifacts rather than the raw
/// onboarding brief. That data has already been through one round of analysis; repeating the raw JSON
/// here would only spend tokens re-deriving what CampaignAnalysis already worked out.</para>
/// </summary>
public static class StrategyPrompt
{
    public static string BuildPositioning(
        TenantBrandProfile brand,
        MarketingCampaign campaign,
        string? campaignAnalysisJson,
        string? marketResearchJson,
        string? competitorResearchJson)
    {
        var lines = new List<string>
        {
            $"You are a marketing strategist writing the positioning for the campaign \"{campaign.Name}\" for the " +
            $"brand \"{brand.Name}\"."
        };

        if (!string.IsNullOrWhiteSpace(campaignAnalysisJson))
        {
            lines.Add("Analysis already produced for this campaign — treat it as established: " + campaignAnalysisJson);
        }
        else
        {
            // Only reachable if campaign analysis was somehow unavailable; without it the model would
            // otherwise have no grounding for what the campaign is trying to do.
            PromptFragments.AddBrandIdentity(lines, brand);
        }

        if (!string.IsNullOrWhiteSpace(marketResearchJson))
        {
            lines.Add(
                "Market research gathered for this campaign. It is best-effort automated research — use it only " +
                "where it genuinely strengthens the positioning and disregard anything that looks off-topic: " +
                marketResearchJson);
        }

        if (!string.IsNullOrWhiteSpace(competitorResearchJson))
        {
            lines.Add(
                "Competitor research gathered for this campaign, subject to the same caveat: " + competitorResearchJson);
        }

        lines.Add(
            "Write, in Arabic: an analysis of the business and its market grounded in everything above, the brand " +
            "strategy this campaign should project, and the marketing strategy for achieving the campaign's " +
            "objective. Each should be a substantial, specific passage, not a bullet list of generic advice.");

        lines.Add(PromptFragments.JsonObjectOnly(
            "{\"businessAndMarketAnalysis\":\"...\",\"brandStrategy\":\"...\",\"marketingStrategy\":\"...\"}"));

        return string.Join(" ", lines);
    }

    public static string BuildBlueprint(
        TenantBrandProfile brand,
        MarketingCampaign campaign,
        string? campaignAnalysisJson,
        string? competitorResearchJson)
    {
        var lines = new List<string>
        {
            $"You are a marketing strategist designing the campaign blueprint for \"{campaign.Name}\" for the brand " +
            $"\"{brand.Name}\"."
        };

        if (!string.IsNullOrWhiteSpace(campaignAnalysisJson))
        {
            lines.Add("Analysis already produced for this campaign — treat it as established: " + campaignAnalysisJson);
        }
        else
        {
            PromptFragments.AddBrandIdentity(lines, brand);
        }

        if (campaign.TargetPlatforms.Count > 0)
        {
            lines.Add($"Target platforms: {string.Join(", ", campaign.TargetPlatforms)}.");
        }

        if (campaign.BudgetAmount.HasValue)
        {
            lines.Add($"Budget: {campaign.BudgetAmount} {campaign.BudgetCurrency}.");
        }

        if (!string.IsNullOrWhiteSpace(competitorResearchJson))
        {
            lines.Add(
                "Competitor research gathered for this campaign. It is best-effort automated research — use it only " +
                "where it genuinely strengthens the blueprint and disregard anything that looks off-topic: " +
                competitorResearchJson);
        }

        lines.Add(
            "Define the campaign's content pillars, its key recurring themes, a posting cadence per platform, a " +
            "content type mix, and which platforms it should actually run on given the target platforms and budget " +
            "above.");

        lines.Add(PromptFragments.JsonObjectOnly(
            "{\"campaignBlueprint\":{\"pillars\":[\"...\"],\"keyThemes\":[\"...\"]," +
            "\"postingCadence\":{\"platform\":\"e.g. 3 posts/week\"},\"contentMix\":{\"contentType\":\"percentage or note\"}," +
            "\"recommendedPlatforms\":[\"...\"]}}"));

        return string.Join(" ", lines);
    }

    public static string BuildRoadmap(
        TenantBrandProfile brand,
        MarketingCampaign campaign,
        string? campaignAnalysisJson,
        string? marketResearchJson,
        string? blueprintJson)
    {
        var lines = new List<string>
        {
            $"You are a marketing strategist writing the execution roadmap for the campaign \"{campaign.Name}\" for " +
            $"the brand \"{brand.Name}\"."
        };

        if (!string.IsNullOrWhiteSpace(campaignAnalysisJson))
        {
            lines.Add("Analysis already produced for this campaign — treat it as established: " + campaignAnalysisJson);
        }
        else
        {
            PromptFragments.AddBrandIdentity(lines, brand);
        }

        if (!string.IsNullOrWhiteSpace(marketResearchJson))
        {
            lines.Add(
                "Market research gathered for this campaign, best-effort and possibly incomplete: " + marketResearchJson);
        }

        if (!string.IsNullOrWhiteSpace(blueprintJson))
        {
            lines.Add(
                "The campaign blueprint already decided for this campaign — build the roadmap on top of it, do not " +
                "contradict it: " + blueprintJson);
        }

        lines.Add(
            "Write, in Arabic: how content production should actually proceed, a phased execution roadmap across the " +
            "campaign's duration, and a short list of AI-specific recommendations (what to automate, what to watch). " +
            "Then write a one-paragraph executive summary of the whole strategy — objective, positioning and plan — " +
            "suitable as the first thing its owner reads before approving it.");

        lines.Add(PromptFragments.JsonObjectOnly(
            "{\"executiveSummary\":\"...\",\"contentProductionPlan\":\"...\",\"executionRoadmap\":\"...\"," +
            "\"aiRecommendations\":[\"...\"]}"));

        return string.Join(" ", lines);
    }
}

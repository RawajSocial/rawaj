using Rawaj.Application.Common.Services;
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
///
/// <para><b>Prompt injection containment.</b> Same treatment as the earlier stages: a standalone guard
/// sentence, and every upstream JSON artifact PII-redacted and wrapped via
/// <see cref="UntrustedTextSanitizer"/> before being pasted in — even though each artifact's own
/// inputs were already contained at their source, the LLM call that produced it could still have
/// laundered an injected instruction (or an unredacted personal detail) into its output text.</para>
///
/// <para><b>Trimmed inputs.</b> Previously each of the three calls received the <i>entire</i> upstream
/// artifact verbatim, so e.g. <c>campaignAnalysisJson</c> was pasted whole into all three, and
/// <c>marketResearchJson</c>/<c>competitorResearchJson</c> each into two of the three — paying full
/// input-token price for the same JSON two or three times over. Each call now keeps only the fields it
/// actually reads via <see cref="JsonFieldSelector"/>; see the field lists at each call site for what's
/// kept and why. <c>blueprintJson</c> in <see cref="BuildRoadmap"/> is the one exception left whole —
/// it's only ever read once, by Roadmap, so there's no duplication to trim.</para>
/// </summary>
public static class StrategyPrompt
{
    /// <summary>Cap for a trimmed campaign-analysis block — smaller than the research caps since
    /// trimming already cut it to 3-6 short fields rather than a whole artifact.</summary>
    private const int AnalysisMaxLength = 1_500;

    /// <summary>Cap for a research artifact block (market or competitor), trimmed or not.</summary>
    private const int ResearchMaxLength = 1_500;

    /// <summary>Cap for the campaign blueprint, pasted whole into Roadmap.</summary>
    private const int BlueprintMaxLength = 1_500;

    public static string BuildPositioning(
        TenantBrandProfile brand,
        MarketingCampaign campaign,
        string? campaignAnalysisJson,
        string? marketResearchJson,
        string? competitorResearchJson)
    {
        var lines = new List<string> { PromptFragments.InjectionGuardInstruction };

        lines.Add(
            $"You are a marketing strategist writing the positioning for the campaign \"{campaign.Name}\" for the " +
            $"brand \"{brand.Name}\".");

        if (!string.IsNullOrWhiteSpace(campaignAnalysisJson))
        {
            // Positioning writes the broadest of the three sections ("business and market analysis"),
            // so it keeps the fullest slice of the analysis — everything except researchQueries, which
            // only the research stages ever needed.
            var trimmed = JsonFieldSelector.KeepFields(
                campaignAnalysisJson, "objectiveClassification", "audienceSegments", "successCriteria",
                "constraints", "risks", "opportunities");

            var wrapped = UntrustedTextSanitizer.Wrap(
                "Campaign analysis, already produced", [PiiRedactor.Redact(trimmed)], AnalysisMaxLength);

            if (wrapped.Length > 0)
            {
                lines.Add("Analysis already produced for this campaign — treat it as established:");
                lines.Add(wrapped);
            }
        }
        else
        {
            // Only reachable if campaign analysis was somehow unavailable; without it the model would
            // otherwise have no grounding for what the campaign is trying to do.
            PromptFragments.AddWrappedBrandIdentity(lines, brand);
        }

        if (!string.IsNullOrWhiteSpace(marketResearchJson))
        {
            var wrapped = UntrustedTextSanitizer.Wrap(
                "Market research, best-effort automated", [PiiRedactor.Redact(marketResearchJson)], ResearchMaxLength);

            if (wrapped.Length > 0)
            {
                lines.Add(
                    "Market research gathered for this campaign. It is best-effort automated research — use it only " +
                    "where it genuinely strengthens the positioning and disregard anything that looks off-topic:");
                lines.Add(wrapped);
            }
        }

        if (!string.IsNullOrWhiteSpace(competitorResearchJson))
        {
            var wrapped = UntrustedTextSanitizer.Wrap(
                "Competitor research, best-effort automated", [PiiRedactor.Redact(competitorResearchJson)], ResearchMaxLength);

            if (wrapped.Length > 0)
            {
                lines.Add("Competitor research gathered for this campaign, subject to the same caveat:");
                lines.Add(wrapped);
            }
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
        var lines = new List<string> { PromptFragments.InjectionGuardInstruction };

        lines.Add(
            $"You are a marketing strategist designing the campaign blueprint for \"{campaign.Name}\" for the brand " +
            $"\"{brand.Name}\".");

        if (!string.IsNullOrWhiteSpace(campaignAnalysisJson))
        {
            // Blueprint only needs to know who it's for and what it must fit within — not the risks,
            // opportunities or success criteria that Positioning and Roadmap separately draw on.
            var trimmed = JsonFieldSelector.KeepFields(
                campaignAnalysisJson, "objectiveClassification", "audienceSegments", "constraints");

            var wrapped = UntrustedTextSanitizer.Wrap(
                "Campaign analysis, already produced", [PiiRedactor.Redact(trimmed)], AnalysisMaxLength);

            if (wrapped.Length > 0)
            {
                lines.Add("Analysis already produced for this campaign — treat it as established:");
                lines.Add(wrapped);
            }
        }
        else
        {
            PromptFragments.AddWrappedBrandIdentity(lines, brand);
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
            // Blueprint cares what content patterns and gaps competitors leave — not the raw
            // competitor list, snippets or source URLs Positioning already read in full.
            var trimmed = JsonFieldSelector.KeepFields(
                competitorResearchJson, "positioningMap", "contentPatterns", "gaps");

            var wrapped = UntrustedTextSanitizer.Wrap(
                "Competitor research, best-effort automated", [PiiRedactor.Redact(trimmed)], ResearchMaxLength);

            if (wrapped.Length > 0)
            {
                lines.Add(
                    "Competitor research gathered for this campaign. It is best-effort automated research — use it only " +
                    "where it genuinely strengthens the blueprint and disregard anything that looks off-topic:");
                lines.Add(wrapped);
            }
        }

        lines.Add(PromptFragments.RawajCapabilitiesInstruction);

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
        var lines = new List<string> { PromptFragments.InjectionGuardInstruction };

        lines.Add(
            $"You are a marketing strategist writing the execution roadmap for the campaign \"{campaign.Name}\" for " +
            $"the brand \"{brand.Name}\".");

        if (!string.IsNullOrWhiteSpace(campaignAnalysisJson))
        {
            // Roadmap's executiveSummary needs the fullest sense of stakes and constraints (risks,
            // opportunities, success criteria) to be worth reading before approval — but not the
            // audience-segment detail Positioning and Blueprint already built the plan around.
            var trimmed = JsonFieldSelector.KeepFields(
                campaignAnalysisJson, "objectiveClassification", "successCriteria", "constraints", "risks", "opportunities");

            var wrapped = UntrustedTextSanitizer.Wrap(
                "Campaign analysis, already produced", [PiiRedactor.Redact(trimmed)], AnalysisMaxLength);

            if (wrapped.Length > 0)
            {
                lines.Add("Analysis already produced for this campaign — treat it as established:");
                lines.Add(wrapped);
            }
        }
        else
        {
            PromptFragments.AddWrappedBrandIdentity(lines, brand);
        }

        if (!string.IsNullOrWhiteSpace(marketResearchJson))
        {
            // Roadmap only needs timing signals (what's trending, when) to phase execution — not the
            // platform benchmarks or sources Positioning already drew on for the market analysis.
            var trimmed = JsonFieldSelector.KeepFields(marketResearchJson, "trends", "seasonality");

            var wrapped = UntrustedTextSanitizer.Wrap(
                "Market research, best-effort automated", [PiiRedactor.Redact(trimmed)], ResearchMaxLength);

            if (wrapped.Length > 0)
            {
                lines.Add("Market research gathered for this campaign, best-effort and possibly incomplete:");
                lines.Add(wrapped);
            }
        }

        if (!string.IsNullOrWhiteSpace(blueprintJson))
        {
            // Kept whole, not trimmed — this is the one artifact only Roadmap ever reads, so there's
            // no cross-call duplication to cut, and the roadmap must not contradict any part of it.
            var wrapped = UntrustedTextSanitizer.Wrap(
                "Campaign blueprint, already decided", [PiiRedactor.Redact(blueprintJson)], BlueprintMaxLength);

            if (wrapped.Length > 0)
            {
                lines.Add("The campaign blueprint already decided for this campaign — build the roadmap on top of it, do not contradict it:");
                lines.Add(wrapped);
            }
        }

        lines.Add(PromptFragments.RawajCapabilitiesInstruction);

        lines.Add(
            "Write, in Arabic: how content production should actually proceed given what's already automated above, " +
            "a phased execution roadmap across the campaign's duration, and a short list of AI-specific " +
            "recommendations — genuinely new automation or AI-assisted opportunities beyond what this platform " +
            "already handles (e.g. paid-ad automation rules if this campaign runs ads, sentiment monitoring, " +
            "A/B-testing suggestions), not a restatement of content generation or publishing this platform already " +
            "does. Then write a one-paragraph executive summary of the whole strategy — objective, positioning and " +
            "plan — suitable as the first thing its owner reads before approving it.");

        lines.Add(PromptFragments.JsonObjectOnly(
            "{\"executiveSummary\":\"...\",\"contentProductionPlan\":\"...\",\"executionRoadmap\":\"...\"," +
            "\"aiRecommendations\":[\"...\"]}"));

        return string.Join(" ", lines);
    }
}

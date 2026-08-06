using Rawaj.Application.Features.AiPipeline.Prompts;
using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Content.Common;

/// <summary>
/// Prompts for the standalone (non-pipeline) generators — single content items, revisions, trial
/// generations — plus the prompts still used by the campaign handlers the pipeline will replace.
///
/// <para>The pipeline's own prompts have moved to <c>Features/AiPipeline/Prompts/</c>, one file per
/// stage, sharing <see cref="PromptFragments"/>. The methods below that a pipeline stage also needs
/// now delegate there, so both paths produce identical text and there is no second copy to drift.</para>
/// </summary>
public static class ContentPromptBuilder
{
    /// <inheritdoc cref="PromptFragments.NoRenderedTextInstruction"/>
    private const string NoRenderedTextInstruction = PromptFragments.NoRenderedTextInstruction;

    private static void AddBrandIdentityLines(List<string> lines, TenantBrandProfile brand) =>
        PromptFragments.AddBrandIdentity(lines, brand);

    public static string BuildTextPrompt(
        TenantBrandProfile brand,
        MarketingCampaign? campaign,
        string contentType,
        string platform,
        string language,
        string? tone,
        string? additionalInstructions,
        ContentTemplateStyle templateStyle = ContentTemplateStyle.Auto)
    {
        var lines = new List<string>
        {
            $"Write a {contentType} for the {platform} platform in {language}.",
            $"Brand: {brand.Name}."
        };

        AddBrandIdentityLines(lines, brand);

        if (campaign is not null)
        {
            lines.Add($"Campaign: {campaign.Name}.");

            if (!string.IsNullOrWhiteSpace(campaign.Objective))
            {
                lines.Add($"Campaign objective: {campaign.Objective}.");
            }
        }

        if (!string.IsNullOrWhiteSpace(tone))
        {
            lines.Add($"Tone: {tone}.");
        }

        if (!string.IsNullOrWhiteSpace(additionalInstructions))
        {
            lines.Add($"Additional instructions: {additionalInstructions}.");
        }

        lines.Add(ContentTemplateCatalog.StructureGuidance(templateStyle));

        lines.Add("Return only the final copy, no explanations or formatting notes.");

        return string.Join(" ", lines);
    }

    public static string BuildRevisionPrompt(
        TenantBrandProfile brand,
        MarketingCampaign? campaign,
        ContentItem contentItem,
        string feedback)
    {
        var lines = new List<string>
        {
            $"Revise the following {contentItem.ContentType} for the {contentItem.Platform} platform in {contentItem.Language}.",
            $"Brand: {brand.Name}."
        };

        if (campaign is not null)
        {
            lines.Add($"Campaign: {campaign.Name}.");
        }

        lines.Add($"Current copy: \"{contentItem.Content}\"");
        lines.Add($"Requested changes: {feedback}");
        lines.Add("Return only the revised copy, no explanations or formatting notes.");

        return string.Join(" ", lines);
    }

    public static string BuildOnboardingQuestionsPrompt(TenantBrandProfile brand, string onboardingContextJson)
    {
        var lines = new List<string>
        {
            $"A business called \"{brand.Name}\" is midway through an onboarding wizard for a social media marketing platform.",
            "Here is the JSON of everything they've entered so far in the wizard (campaign type, brand details, target audience, positioning, budget, etc.):",
            onboardingContextJson,
            "Based specifically on what this business entered, write between 3 and 8 short follow-up questions in Arabic (choose however many are actually needed to fill the gaps - don't pad the count) " +
            "that would help a marketing strategist understand their business better and fill in gaps the answers above didn't cover. Each question needs 3 short quick-reply suggested answers in Arabic, relevant to what THIS business described - not generic questions.",
            "Respond with ONLY a valid JSON array (no markdown fences, no commentary) with this exact shape: " +
            "[{\"question\":\"...\",\"suggestions\":[\"...\",\"...\",\"...\"]}]",
        };

        return string.Join(" ", lines);
    }

    /// <summary>
    /// "What we understood about your business" — the AI Business Diagnosis pricing-sheet feature.
    /// Combines the onboarding brief with (best-effort, possibly-empty) competitor research into a
    /// diagnosis the user sees and confirms before a strategy is built on top of it.
    /// </summary>
    public static string BuildBusinessDiagnosisPrompt(TenantBrandProfile brand, string? briefJson, string? competitorJson)
    {
        var lines = new List<string>
        {
            $"You are a marketing strategist producing a business diagnosis for \"{brand.Name}\" that its owner will read and confirm before any campaign strategy is built."
        };

        AddBrandIdentityLines(lines, brand);

        if (!string.IsNullOrWhiteSpace(briefJson))
        {
            lines.Add(
                "Here is the JSON of everything they entered in the onboarding wizard (campaign type, brand details, " +
                "target audience, positioning, budget, and their answers to AI follow-up questions):");
            lines.Add(briefJson);
        }

        if (!string.IsNullOrWhiteSpace(competitorJson))
        {
            lines.Add(
                "Here is automated competitor research gathered for this business. It is best-effort and may or may " +
                "not be relevant or accurate — weigh it accordingly and ignore anything that looks off-topic:");
            lines.Add(competitorJson);
        }

        lines.Add(
            "Write, in Arabic, a plain-language summary of what this business is and does, a SWOT analysis, its business " +
            "maturity stage, its growth stage, how ready its marketing currently is, the key risks it currently faces, " +
            "the opportunities it should pursue, and anything important that's still missing from what they told you.");

        lines.Add(
            "Respond with ONLY a valid JSON object (no markdown fences, no commentary) with this exact shape: " +
            "{\"businessSummary\":\"...\",\"swot\":{\"strengths\":[\"...\"],\"weaknesses\":[\"...\"],\"opportunities\":[\"...\"],\"threats\":[\"...\"]}," +
            "\"businessMaturity\":\"...\",\"growthStage\":\"...\",\"marketingReadiness\":\"...\",\"currentRisks\":[\"...\"]," +
            "\"opportunities\":[\"...\"],\"missingInformation\":[\"...\"]}");

        return string.Join(" ", lines);
    }

    /// <summary>
    /// The Complete Marketing Strategy pricing-sheet feature. Grounds the strategy in the onboarding
    /// brief and the business diagnosis (both already confirmed by the user), and folds in competitor
    /// research as advisory input only — it may or may not be relevant, and the model is told to
    /// disregard it where it isn't rather than force-fit it into the plan.
    /// </summary>
    public static string BuildCampaignStrategyPrompt(
        TenantBrandProfile brand,
        MarketingCampaign campaign,
        string? briefJson,
        string? diagnosisJson,
        string? competitorJson)
    {
        var lines = new List<string>
        {
            $"Create a complete marketing strategy for the campaign \"{campaign.Name}\" for the brand \"{brand.Name}\"."
        };

        AddBrandIdentityLines(lines, brand);

        if (!string.IsNullOrWhiteSpace(campaign.Objective))
        {
            lines.Add($"Campaign objective: {campaign.Objective}.");
        }

        if (campaign.TargetPlatforms.Count > 0)
        {
            lines.Add($"Target platforms: {string.Join(", ", campaign.TargetPlatforms)}.");
        }

        if (campaign.StartDate.HasValue && campaign.EndDate.HasValue)
        {
            lines.Add($"Campaign runs from {campaign.StartDate} to {campaign.EndDate}.");
        }

        if (campaign.BudgetAmount.HasValue)
        {
            lines.Add($"Budget: {campaign.BudgetAmount} {campaign.BudgetCurrency}.");
        }

        if (!string.IsNullOrWhiteSpace(briefJson))
        {
            lines.Add("Onboarding brief JSON: " + briefJson);
        }

        if (!string.IsNullOrWhiteSpace(diagnosisJson))
        {
            lines.Add("Business diagnosis already produced and confirmed for this business — use it as grounding: " + diagnosisJson);
        }

        if (!string.IsNullOrWhiteSpace(competitorJson))
        {
            lines.Add(
                "Competitor research gathered for this campaign. It is best-effort automated research — it may or may " +
                "not be relevant, so use it only where it genuinely strengthens the strategy and disregard it otherwise: " +
                competitorJson);
        }

        lines.Add(
            "Respond with ONLY a valid JSON object (no markdown fences, no commentary) with this exact shape: " +
            "{\"executiveSummary\":\"...\",\"businessAndMarketAnalysis\":\"...\",\"brandStrategy\":\"...\",\"marketingStrategy\":\"...\"," +
            "\"campaignBlueprint\":{\"pillars\":[\"...\"],\"keyThemes\":[\"...\"],\"postingCadence\":{\"platform\":\"e.g. 3 posts/week\"}," +
            "\"contentMix\":{\"contentType\":\"percentage or note\"},\"recommendedPlatforms\":[\"...\"]}," +
            "\"contentProductionPlan\":\"...\",\"executionRoadmap\":\"...\",\"aiRecommendations\":[\"...\"]}");

        return string.Join(" ", lines);
    }

    /// <summary>
    /// The campaign content batch. Grounded in the <b>approved strategy</b> (and the onboarding
    /// brief behind it), not just the brand's identity fields — the user pays for and explicitly
    /// approves that strategy, and content generation is gated on the approval, so generating posts
    /// that ignore it would make the whole approval step decorative.
    /// </summary>
    /// <param name="strategyJson">
    /// <c>MarketingCampaign.AiPlanJson</c> — the approved strategy. Its <c>campaignBlueprint</c>
    /// (pillars, key themes, posting cadence, content mix) is what the posts must actually follow.
    /// </param>
    /// <param name="briefJson">
    /// <c>MarketingCampaign.BriefJson</c> — the raw onboarding answers plus the AI follow-up
    /// questions. Supplies audience/tone specifics that the strategy summarises but doesn't repeat
    /// verbatim. The business diagnosis is deliberately *not* passed as well: the strategy was
    /// already built on top of it, so including it again would spend tokens re-stating grounding
    /// the strategy has absorbed.
    /// </param>
    /// <inheritdoc cref="ContentPlanPrompt.Build"/>
    public static string BuildCampaignContentPlanPrompt(
        TenantBrandProfile brand,
        MarketingCampaign campaign,
        List<string> competitorInsights,
        string postingTimeSummary,
        int postCount,
        List<SocialPlatform> platforms,
        Language language,
        string? strategyJson = null,
        string? briefJson = null,
        ContentTemplateStyle templateStyle = ContentTemplateStyle.Auto) =>
        ContentPlanPrompt.Build(
            brand, campaign, competitorInsights, postingTimeSummary, postCount, platforms, language,
            strategyJson, briefJson, templateStyle);

    /// <summary>
    /// Refines an already-generated campaign strategy by free-text feedback, mirroring how
    /// BuildRevisionPrompt handles feedback-driven revision for a single content item — the model
    /// is asked to keep the same JSON shape so the approval UI keeps rendering it unchanged.
    /// </summary>
    public static string BuildPlanRefinementPrompt(
        TenantBrandProfile brand,
        MarketingCampaign campaign,
        string currentPlanJson,
        string feedback)
    {
        var lines = new List<string>
        {
            $"Revise the following marketing strategy for the campaign \"{campaign.Name}\" for the brand \"{brand.Name}\".",
            $"Current strategy JSON: {currentPlanJson}",
            $"Requested changes: {feedback}",
            "Keep everything that wasn't asked to change, and apply the requested changes precisely.",
            "Respond with ONLY a valid JSON object (no markdown fences, no commentary) using the exact same shape as the current strategy JSON above."
        };

        return string.Join(" ", lines);
    }

    /// <param name="userPrompt">
    /// What the picture should show. For campaign posts this is the model-authored English
    /// <c>ContentItem.ImagePrompt</c>, not the post copy — see that field's remarks. For a
    /// user-typed prompt it is whatever they wrote, in whatever language; the closing instruction
    /// below tells the model to interpret it and render an English-described scene rather than
    /// attempting to typeset the words.
    /// </param>
    public static string BuildImagePrompt(
        TenantBrandProfile brand,
        MarketingCampaign? campaign,
        string visualType,
        string userPrompt,
        ContentTemplateStyle templateStyle = ContentTemplateStyle.Auto) =>
        VisualPrompt.Build(brand, campaign, visualType, userPrompt, templateStyle);

    /// <summary>Text prompt for a "standalone" generation with no brand profile (trying the product
    /// out) — same shape as <see cref="BuildTextPrompt"/> minus every brand-identity line.</summary>
    public static string BuildStandaloneTextPrompt(
        string contentType,
        string platform,
        string language,
        string? tone,
        string? additionalInstructions,
        ContentTemplateStyle templateStyle = ContentTemplateStyle.Auto)
    {
        var lines = new List<string>
        {
            $"Write a {contentType} for the {platform} platform in {language}.",
            "No specific brand is set — write generic, broadly applicable marketing copy."
        };

        if (!string.IsNullOrWhiteSpace(tone))
        {
            lines.Add($"Tone: {tone}.");
        }

        if (!string.IsNullOrWhiteSpace(additionalInstructions))
        {
            lines.Add($"Additional instructions: {additionalInstructions}.");
        }

        lines.Add(ContentTemplateCatalog.StructureGuidance(templateStyle));

        lines.Add("Return only the final copy, no explanations or formatting notes.");

        return string.Join(" ", lines);
    }

    /// <summary>Image prompt for a "standalone" generation with no brand profile — see
    /// <see cref="BuildStandaloneTextPrompt"/>.</summary>
    public static string BuildStandaloneImagePrompt(string visualType, string userPrompt, ContentTemplateStyle templateStyle = ContentTemplateStyle.Auto) =>
        VisualPrompt.BuildStandalone(visualType, userPrompt, templateStyle);
}

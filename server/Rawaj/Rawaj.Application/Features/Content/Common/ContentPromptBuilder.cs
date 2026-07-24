using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Content.Common;

public static class ContentPromptBuilder
{
    private static void AddBrandIdentityLines(List<string> lines, TenantBrandProfile brand)
    {
        if (!string.IsNullOrWhiteSpace(brand.Description))
        {
            lines.Add($"Brand description: {brand.Description}.");
        }

        if (!string.IsNullOrWhiteSpace(brand.BrandInfo?.Tagline))
        {
            lines.Add($"Brand tagline: {brand.BrandInfo.Tagline}.");
        }

        if (!string.IsNullOrWhiteSpace(brand.BrandInfo?.Industry))
        {
            lines.Add($"Industry: {brand.BrandInfo.Industry}.");
        }

        if (!string.IsNullOrWhiteSpace(brand.BrandInfo?.TargetAudience))
        {
            lines.Add($"Target audience: {brand.BrandInfo.TargetAudience}.");
        }

        if (brand.BrandInfo?.Keywords is { Count: > 0 })
        {
            lines.Add($"Relevant keywords: {string.Join(", ", brand.BrandInfo.Keywords)}.");
        }

        if (brand.BrandVoice.HasValue)
        {
            lines.Add($"Brand voice: {brand.BrandVoice}.");
        }
    }

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

    public static string BuildCampaignContentPlanPrompt(
        TenantBrandProfile brand,
        MarketingCampaign campaign,
        List<string> competitorInsights,
        string postingTimeSummary,
        int postCount,
        List<SocialPlatform> platforms,
        Language language,
        ContentTemplateStyle templateStyle = ContentTemplateStyle.Auto)
    {
        var lines = new List<string>
        {
            $"Create {postCount} distinct social media posts for the campaign \"{campaign.Name}\" for the brand \"{brand.Name}\", " +
            $"written in {language}, distributed across these platforms: {string.Join(", ", platforms)}."
        };

        AddBrandIdentityLines(lines, brand);

        if (!string.IsNullOrWhiteSpace(campaign.Objective))
        {
            lines.Add($"Campaign objective: {campaign.Objective}.");
        }

        if (campaign.StartDate.HasValue && campaign.EndDate.HasValue)
        {
            lines.Add($"Campaign runs from {campaign.StartDate} to {campaign.EndDate}.");
        }

        if (competitorInsights.Count > 0)
        {
            lines.Add("Known competitor intelligence: " + string.Join(" | ", competitorInsights));
        }

        if (!string.IsNullOrWhiteSpace(postingTimeSummary))
        {
            lines.Add("Optimal posting time guidance: " + postingTimeSummary);
        }

        lines.Add(
            $"For each post choose a dayOffset (integer, 0 = campaign start day, {postCount * 2} = latest allowed) and an hour " +
            "(0-23) that best matches the posting time guidance above. contentType must be one of: " +
            "Post, Story, ReelScript, AdCopy, Blog, Caption. platform must be one of the target platforms listed above.");

        lines.Add(ContentTemplateCatalog.StructureGuidance(templateStyle));

        lines.Add(
            "Respond with ONLY a valid JSON object (no markdown fences, no commentary) with this exact shape: " +
            "{\"posts\":[{\"platform\":\"...\",\"contentType\":\"...\",\"dayOffset\":0,\"hour\":18,\"content\":\"...\"," +
            "\"hashtags\":[\"...\"],\"cta\":\"...\"}]}. " +
            $"Return exactly {postCount} posts in the array.");

        return string.Join(" ", lines);
    }

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

    public static string BuildImagePrompt(
        TenantBrandProfile brand,
        MarketingCampaign? campaign,
        string visualType,
        string userPrompt,
        ContentTemplateStyle templateStyle = ContentTemplateStyle.Auto)
    {
        var lines = new List<string> { userPrompt, $"Style fits a {visualType} for the brand \"{brand.Name}\"." };

        if (!string.IsNullOrWhiteSpace(brand.BrandInfo?.Industry))
        {
            lines.Add($"Industry: {brand.BrandInfo.Industry}.");
        }

        if (brand.BrandInfo?.Colors is { Count: > 0 })
        {
            lines.Add($"Use brand colors: {string.Join(", ", brand.BrandInfo.Colors)}.");
        }

        if (brand.BrandInfo?.Keywords is { Count: > 0 })
        {
            lines.Add($"Relevant keywords: {string.Join(", ", brand.BrandInfo.Keywords)}.");
        }

        if (campaign is not null)
        {
            lines.Add($"Campaign context: {campaign.Name}.");
        }

        lines.Add(ContentTemplateCatalog.ImageStyleHint(templateStyle));

        return string.Join(" ", lines);
    }
}

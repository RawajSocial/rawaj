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
        MarketingCampaign campaign,
        ContentItem contentItem,
        string feedback)
    {
        var lines = new List<string>
        {
            $"Revise the following {contentItem.ContentType} for the {contentItem.Platform} platform in {contentItem.Language}.",
            $"Brand: {brand.Name}.",
            $"Campaign: {campaign.Name}.",
            $"Current copy: \"{contentItem.Content}\"",
            $"Requested changes: {feedback}",
            "Return only the revised copy, no explanations or formatting notes."
        };

        return string.Join(" ", lines);
    }

    public static string BuildOnboardingQuestionsPrompt(TenantBrandProfile brand, string onboardingContextJson)
    {
        var lines = new List<string>
        {
            $"A business called \"{brand.Name}\" is midway through an onboarding wizard for a social media marketing platform.",
            "Here is the JSON of everything they've entered so far in the wizard (campaign type, brand details, target audience, positioning, budget, etc.):",
            onboardingContextJson,
            "Based specifically on what this business entered, write exactly 5 short follow-up questions in Arabic that would help a marketing strategist understand their business better " +
            "and fill in gaps the answers above didn't cover. Each question needs 3 short quick-reply suggested answers in Arabic, relevant to what THIS business described - not generic questions.",
            "Respond with ONLY a valid JSON array (no markdown fences, no commentary) with this exact shape: " +
            "[{\"question\":\"...\",\"suggestions\":[\"...\",\"...\",\"...\"]}]",
        };

        return string.Join(" ", lines);
    }

    public static string BuildMarketingPlanPrompt(
        TenantBrandProfile brand,
        MarketingCampaign campaign,
        List<string> competitorInsights)
    {
        var lines = new List<string>
        {
            $"Create a marketing strategy plan for the campaign \"{campaign.Name}\" for the brand \"{brand.Name}\"."
        };

        if (!string.IsNullOrWhiteSpace(brand.BrandInfo?.Industry))
        {
            lines.Add($"Industry: {brand.BrandInfo.Industry}.");
        }

        if (!string.IsNullOrWhiteSpace(brand.BrandInfo?.TargetAudience))
        {
            lines.Add($"Target audience: {brand.BrandInfo.TargetAudience}.");
        }

        if (brand.BrandVoice.HasValue)
        {
            lines.Add($"Brand voice: {brand.BrandVoice}.");
        }

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

        if (competitorInsights.Count > 0)
        {
            lines.Add("Known competitor intelligence: " + string.Join(" | ", competitorInsights));
        }

        lines.Add(
            "Respond with ONLY a valid JSON object (no markdown fences, no commentary) with this exact shape: " +
            "{\"pillars\":[\"...\"],\"keyThemes\":[\"...\"],\"postingCadence\":{\"platform\":\"e.g. 3 posts/week\"}," +
            "\"contentMix\":{\"contentType\":\"percentage or note\"},\"recommendedPlatforms\":[\"...\"],\"summary\":\"...\"}");

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

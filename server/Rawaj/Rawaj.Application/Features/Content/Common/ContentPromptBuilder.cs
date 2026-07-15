using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.Entities.Tenants;

namespace Rawaj.Application.Features.Content.Common;

public static class ContentPromptBuilder
{
    public static string BuildTextPrompt(
        TenantBrandProfile brand,
        MarketingCampaign campaign,
        string contentType,
        string platform,
        string language,
        string? tone,
        string? additionalInstructions)
    {
        var lines = new List<string>
        {
            $"Write a {contentType} for the {platform} platform in {language}.",
            $"Brand: {brand.Name}."
        };

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

        if (brand.BrandVoice.HasValue)
        {
            lines.Add($"Brand voice: {brand.BrandVoice}.");
        }

        lines.Add($"Campaign: {campaign.Name}.");

        if (!string.IsNullOrWhiteSpace(campaign.Objective))
        {
            lines.Add($"Campaign objective: {campaign.Objective}.");
        }

        if (!string.IsNullOrWhiteSpace(tone))
        {
            lines.Add($"Tone: {tone}.");
        }

        if (!string.IsNullOrWhiteSpace(additionalInstructions))
        {
            lines.Add($"Additional instructions: {additionalInstructions}.");
        }

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

    public static string BuildImagePrompt(
        TenantBrandProfile brand,
        MarketingCampaign campaign,
        string visualType,
        string userPrompt)
    {
        var lines = new List<string> { userPrompt, $"Style fits a {visualType} for the brand \"{brand.Name}\"." };

        if (!string.IsNullOrWhiteSpace(brand.BrandInfo?.Industry))
        {
            lines.Add($"Industry: {brand.BrandInfo.Industry}.");
        }

        lines.Add($"Campaign context: {campaign.Name}.");

        return string.Join(" ", lines);
    }
}

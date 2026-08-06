using Rawaj.Application.Features.Content.Common;
using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.AiPipeline.Prompts;

/// <summary>
/// Image prompts — used by the <c>ContentImage</c> stage and, unchanged, by the standalone
/// visual-asset generator.
/// </summary>
public static class VisualPrompt
{
    /// <param name="userPrompt">
    /// What the picture should show. For campaign posts this is the model-authored English
    /// <c>ContentItem.ImagePrompt</c>, not the post copy — see that field's remarks. For a
    /// user-typed prompt it is whatever they wrote, in whatever language; the closing instruction
    /// tells the model to interpret it and render an English-described scene rather than attempting
    /// to typeset the words.
    /// </param>
    public static string Build(
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
        lines.Add(PromptFragments.NoRenderedTextInstruction);

        return string.Join(" ", lines);
    }

    /// <summary>Image prompt for a generation with no brand profile (trying the product out) — the
    /// same shape minus every brand-identity line.</summary>
    public static string BuildStandalone(
        string visualType, string userPrompt, ContentTemplateStyle templateStyle = ContentTemplateStyle.Auto)
    {
        var lines = new List<string> { userPrompt, $"Style fits a {visualType}." };
        lines.Add(ContentTemplateCatalog.ImageStyleHint(templateStyle));
        lines.Add(PromptFragments.NoRenderedTextInstruction);
        return string.Join(" ", lines);
    }
}

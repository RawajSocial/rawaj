using Rawaj.Application.Features.Content.Common;
using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.AiPipeline.Prompts;

/// <summary>
/// The campaign content batch — the <c>ContentPlan</c> stage's prompt.
///
/// <para>Grounded in the <b>approved strategy</b> (and the onboarding brief behind it), not just the
/// brand's identity fields. The user pays for and explicitly approves that strategy, and content
/// generation is gated on the approval, so generating posts that ignore it would make the whole
/// approval step decorative. <c>CampaignContentPromptTests</c> locks this in, because nothing fails
/// loudly when it regresses — the output just quietly stops following the plan.</para>
/// </summary>
public static class ContentPlanPrompt
{
    /// <param name="strategyJson">The approved strategy. Its <c>campaignBlueprint</c> (pillars, key
    /// themes, posting cadence, content mix) is what the posts must actually follow.</param>
    /// <param name="briefJson">The raw onboarding answers plus the AI follow-up questions. Supplies
    /// audience and tone specifics the strategy summarises but doesn't repeat verbatim. The business
    /// diagnosis is deliberately <i>not</i> passed as well: the strategy was already built on top of
    /// it, so including it again would spend tokens re-stating grounding the strategy has absorbed.</param>
    public static string Build(
        TenantBrandProfile brand,
        MarketingCampaign campaign,
        List<string> competitorInsights,
        string postingTimeSummary,
        int postCount,
        List<SocialPlatform> platforms,
        Language language,
        string? strategyJson = null,
        string? briefJson = null,
        ContentTemplateStyle templateStyle = ContentTemplateStyle.Auto)
    {
        var lines = new List<string>
        {
            $"Create {postCount} distinct social media posts for the campaign \"{campaign.Name}\" for the brand \"{brand.Name}\", " +
            $"written in {language}, distributed across these platforms: {string.Join(", ", platforms)}."
        };

        PromptFragments.AddBrandIdentity(lines, brand);

        if (!string.IsNullOrWhiteSpace(campaign.Objective))
        {
            lines.Add($"Campaign objective: {campaign.Objective}.");
        }

        if (campaign.StartDate.HasValue && campaign.EndDate.HasValue)
        {
            lines.Add($"Campaign runs from {campaign.StartDate} to {campaign.EndDate}.");
        }

        if (!string.IsNullOrWhiteSpace(strategyJson))
        {
            lines.Add(
                "This campaign already has a marketing strategy that the business owner reviewed and approved. " +
                "These posts are the execution of that strategy, so follow it: draw the themes from its " +
                "campaignBlueprint.pillars and keyThemes, respect its contentMix when choosing each post's " +
                "contentType, and keep the voice consistent with its brandStrategy and marketingStrategy. " +
                "Approved strategy JSON: " + strategyJson);
        }

        if (!string.IsNullOrWhiteSpace(briefJson))
        {
            lines.Add(
                "The business's own onboarding answers, including their replies to AI follow-up questions — use " +
                "these for audience and tone specifics: " + briefJson);
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

        // The image model is trained on English captions and renders Arabic prompts poorly, and the
        // post copy itself is persuasion, not a description of a picture — so the model that writes
        // the post also writes a separate English description of the image to accompany it.
        lines.Add(
            "For each post also write an \"imagePrompt\": a description of the photo or illustration that should " +
            "accompany it, for a text-to-image model. The imagePrompt MUST be written in English even when the post " +
            "copy is in another language, and it must describe what is literally visible in the picture — subject, " +
            "setting, composition, lighting, mood, style — not the marketing message. Do not put slogans, calls to " +
            "action, hashtags, prices or any text-to-be-rendered in it. Keep it under 60 words.");

        lines.Add(ContentTemplateCatalog.StructureGuidance(templateStyle));

        lines.Add(
            PromptFragments.JsonObjectOnly(
                "{\"posts\":[{\"platform\":\"...\",\"contentType\":\"...\",\"dayOffset\":0,\"hour\":18,\"content\":\"...\"," +
                "\"hashtags\":[\"...\"],\"cta\":\"...\",\"imagePrompt\":\"...\"}]}. ") +
            $"Return exactly {postCount} posts in the array.");

        return string.Join(" ", lines);
    }
}

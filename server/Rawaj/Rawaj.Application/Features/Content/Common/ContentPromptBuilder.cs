using Rawaj.Application.Common.Services;
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

    private const int RevisionStrategyMaxLength = 1_200;
    private const int RevisionBriefMaxLength = 1_200;
    private static readonly string[] RevisionStrategyFieldsNeeded = ["campaignBlueprint", "brandStrategy", "marketingStrategy"];

    /// <summary>
    /// Full "remake" of a single post — unlike <see cref="BuildRevisionPrompt"/> (which only ever
    /// had the current copy and the feedback text to go on), this carries the same grounding a fresh
    /// campaign post gets: brand identity, campaign objective, and the approved strategy/brief — so a
    /// remake stays on-theme instead of drifting into something unrelated to the rest of the
    /// campaign. Also asks for a fresh imagePrompt so the caller can regenerate the photo alongside
    /// the copy, keeping the two paired the way they were at original generation.
    /// </summary>
    public static string BuildFullRevisionPrompt(
        TenantBrandProfile brand,
        MarketingCampaign? campaign,
        ContentItem contentItem,
        string feedback)
    {
        var lines = new List<string> { PromptFragments.InjectionGuardInstruction };

        lines.Add($"Revise the following {contentItem.ContentType} for the {contentItem.Platform} platform in {contentItem.Language}.");

        PromptFragments.AddWrappedBrandIdentity(lines, brand);

        if (campaign is not null)
        {
            lines.Add($"Campaign: {campaign.Name}.");

            if (!string.IsNullOrWhiteSpace(campaign.Objective))
            {
                var wrappedObjective = UntrustedTextSanitizer.Wrap(
                    "Campaign objective, typed by the business", [PiiRedactor.Redact(campaign.Objective)]);

                if (wrappedObjective.Length > 0)
                {
                    lines.Add(wrappedObjective);
                }
            }

            if (!string.IsNullOrWhiteSpace(campaign.AiPlanJson))
            {
                var trimmedStrategy = JsonFieldSelector.KeepFields(campaign.AiPlanJson, RevisionStrategyFieldsNeeded);
                var wrappedStrategy = UntrustedTextSanitizer.Wrap(
                    "Approved strategy JSON", [PiiRedactor.Redact(trimmedStrategy!)], RevisionStrategyMaxLength);

                if (wrappedStrategy.Length > 0)
                {
                    lines.Add(
                        "This post belongs to a campaign with an approved marketing strategy — keep the revision on " +
                        "theme with its campaignBlueprint.pillars/keyThemes and consistent with its brandStrategy " +
                        "and marketingStrategy.");
                    lines.Add(wrappedStrategy);
                }
            }

            if (!string.IsNullOrWhiteSpace(campaign.BriefJson))
            {
                var wrappedBrief = UntrustedTextSanitizer.Wrap(
                    "The business's own onboarding answers, for audience and tone specifics", [PiiRedactor.Redact(campaign.BriefJson)], RevisionBriefMaxLength);

                if (wrappedBrief.Length > 0)
                {
                    lines.Add(wrappedBrief);
                }
            }
        }

        lines.Add($"Current copy: \"{contentItem.Content}\"");
        lines.Add($"Requested changes: {feedback}");

        lines.Add(
            "For each post also write an \"imagePrompt\": a description of the photo or illustration that should " +
            "accompany the revised copy, for a text-to-image model. The imagePrompt MUST be written in English " +
            "even when the post copy is in another language, and it must describe what is literally visible in " +
            "the picture — subject, setting, composition, lighting, mood, style — not the marketing message. Do " +
            "not put slogans, calls to action, hashtags, prices or any text-to-be-rendered in it. Keep it under " +
            "60 words.");

        lines.Add(
            PromptFragments.JsonObjectOnly(
                "{\"content\":\"...\",\"hashtags\":[\"...\"],\"cta\":\"...\",\"imagePrompt\":\"...\"}"));

        return string.Join(" ", lines);
    }

    private const int OnboardingContextMaxLength = 2_000;

    /// <summary>
    /// The wizard's step-7 follow-up questions and their quick-reply suggestions.
    ///
    /// <para><b>Prompt injection containment.</b> <c>onboardingContextJson</c> is the most directly
    /// tenant-authored input in the app — open free-text answers from every earlier wizard step,
    /// concatenated into one JSON blob — yet this prompt lived outside <c>Features/AiPipeline/Prompts/</c>
    /// and was missed by the earlier injection-governance pass. Same treatment as the rest now: guard
    /// sentence, PII redaction, wrapping.</para>
    ///
    /// <para><b>Grounding.</b> Also carries <see cref="PromptFragments.RawajCapabilitiesInstruction"/>
    /// plus an explicit content-type constraint: without it, suggested answers can offer things like
    /// "video ads" that this platform cannot produce (it generates image posts/stories, not video).
    /// The "don't repeat" instruction below is stated as a hard constraint rather than the previous
    /// soft "fill the gaps" phrasing, which the model wasn't reliably honoring.</para>
    /// </summary>
    public static string BuildOnboardingQuestionsPrompt(TenantBrandProfile brand, string onboardingContextJson)
    {
        var lines = new List<string>
        {
            PromptFragments.InjectionGuardInstruction,
            $"A business called \"{brand.Name}\" is midway through an onboarding wizard for a social media marketing platform.",
        };

        // Stated plainly, in the opening sentence, same treatment as brand.Name just above (short,
        // low injection-surface tenant field, unwrapped by existing precedent) — not just left to the
        // wrapped bullet list AddWrappedBrandIdentity adds below. The wizard itself never asks for a
        // city/country at all (a business only ever types that on its brand profile, not in this
        // flow), and a location buried as one bullet among seven others turned out not to be a strong
        // enough signal to override the model's own training bias: even with Location present in the
        // wrapped block, it kept assuming Saudi Arabia for a Cairo, Egypt business. Putting it up
        // front, stated as fact rather than data-to-extract, is the stronger version of the same fix.
        if (!string.IsNullOrWhiteSpace(brand.BrandInfo?.Location))
        {
            lines.Add(
                $"This business is located in {PiiRedactor.Redact(brand.BrandInfo.Location)}. Every question and " +
                "suggested answer must be consistent with operating in that specific location — do not assume any " +
                "other country or city (including Saudi Arabia) unless the business's own answers below say otherwise.");
        }

        PromptFragments.AddWrappedBrandIdentity(lines, brand);

        var wrappedContext = UntrustedTextSanitizer.Wrap(
            "Everything they've entered so far in the wizard (campaign type, brand details, target audience, " +
            "positioning, budget, etc.), as JSON",
            [PiiRedactor.Redact(onboardingContextJson)],
            OnboardingContextMaxLength);

        if (wrappedContext.Length > 0)
        {
            lines.Add(wrappedContext);
        }

        lines.Add(PromptFragments.RawajCapabilitiesInstruction);
        lines.Add(
            "This platform produces image-based content only (posts, stories, static ad creatives) — it does not " +
            "produce or edit video. Never suggest video, reels footage, or video ads as a quick-reply answer.");
        lines.Add(PromptFragments.ArabicOnlyInstruction);
        lines.Add(
            "Based specifically on what this business entered, write between 3 and 8 short follow-up questions " +
            "(choose however many are actually needed to fill the gaps - don't pad the count) " +
            "that would help a marketing strategist understand their business better. Do NOT ask about anything " +
            "already answered in the JSON above (e.g. if a target gender, budget, or platform was already given, " +
            "do not ask for it again) - only ask about genuine gaps. Each question needs 3 short quick-reply " +
            "suggested answers, relevant to what THIS business described - not generic questions, and never " +
            "suggesting a capability this platform doesn't have.");
        lines.Add(PromptFragments.ArabicOnlyInstruction);
        lines.Add(
            "Respond with ONLY a valid JSON array (no markdown fences, no commentary) with this exact shape: " +
            "[{\"question\":\"...\",\"suggestions\":[\"...\",\"...\",\"...\"]}]");

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
        string postingTimeSummary,
        int postCount,
        List<SocialPlatform> platforms,
        Language language,
        string? strategyJson = null,
        string? briefJson = null,
        ContentTemplateStyle templateStyle = ContentTemplateStyle.Auto) =>
        ContentPlanPrompt.Build(
            brand, campaign, postingTimeSummary, postCount, platforms, language,
            strategyJson, briefJson, templateStyle);

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

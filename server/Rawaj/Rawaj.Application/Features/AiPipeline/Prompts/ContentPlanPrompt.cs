using Rawaj.Application.Common.Services;
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
///
/// <para><b>Prompt injection containment.</b> This call runs on every content batch — not a rare
/// fallback like the other prompts' <c>AddBrandIdentity</c> branch — so it's the highest-traffic path
/// that was still sending raw tenant/upstream text unwrapped. Same treatment as the rest of the
/// pipeline now: a standalone guard sentence, brand identity via
/// <see cref="PromptFragments.AddWrappedBrandIdentity"/>, and the campaign objective/strategy/brief all
/// PII-redacted and wrapped via <see cref="UntrustedTextSanitizer"/>. Competitor insights are wrapped
/// too but deliberately not PII-redacted — they're raw excerpts from third-party web pages (see
/// <c>CompetitorResearchExecutor</c>'s <c>RagDocument</c> rows), the same "expected, on-topic, not our
/// tenant's data" category as <c>ResearchSynthesisPrompt</c>'s excerpts.</para>
/// </summary>
public static class ContentPlanPrompt
{
    private const int StrategyMaxLength = 1_200;
    private const int BriefMaxLength = 1_200;

    private static readonly string[] StrategyFieldsNeeded = ["campaignBlueprint", "brandStrategy", "marketingStrategy"];

    /// <param name="strategyJson">The approved strategy. Trimmed to <see cref="StrategyFieldsNeeded"/>
    /// before it reaches the model: <c>campaignBlueprint</c> (pillars, key themes, posting cadence,
    /// content mix) is what the posts must actually follow, and <c>brandStrategy</c>/
    /// <c>marketingStrategy</c> keep the voice consistent. The rest of the strategy object
    /// (<c>executiveSummary</c>, <c>businessAndMarketAnalysis</c>, <c>aiRecommendations</c>) is
    /// grounding the strategy stages already absorbed when they built this JSON — resending it here
    /// was pure token cost with nothing this stage acts on.</param>
    /// <param name="briefJson">The raw onboarding answers plus the AI follow-up questions. Supplies
    /// audience and tone specifics the strategy summarises but doesn't repeat verbatim. The business
    /// diagnosis is deliberately <i>not</i> passed as well: the strategy was already built on top of
    /// it, so including it again would spend tokens re-stating grounding the strategy has absorbed.</param>
    public static string Build(
        TenantBrandProfile brand,
        MarketingCampaign campaign,
        string postingTimeSummary,
        int postCount,
        List<SocialPlatform> platforms,
        Language language,
        string? strategyJson = null,
        string? briefJson = null,
        ContentTemplateStyle templateStyle = ContentTemplateStyle.Auto)
    {
        var lines = new List<string> { PromptFragments.InjectionGuardInstruction };

        lines.Add(
            $"Create {postCount} distinct social media posts for the campaign \"{campaign.Name}\" for the brand \"{brand.Name}\", " +
            $"written in {language}, distributed across these platforms: {string.Join(", ", platforms)}.");

        PromptFragments.AddWrappedBrandIdentity(lines, brand);

        if (!string.IsNullOrWhiteSpace(campaign.Objective))
        {
            var wrappedObjective = UntrustedTextSanitizer.Wrap(
                "Campaign objective, typed by the business", [PiiRedactor.Redact(campaign.Objective)]);

            if (wrappedObjective.Length > 0)
            {
                lines.Add(wrappedObjective);
            }
        }

        if (campaign.StartDate.HasValue && campaign.EndDate.HasValue)
        {
            lines.Add($"Campaign runs from {campaign.StartDate} to {campaign.EndDate}.");
        }

        if (!string.IsNullOrWhiteSpace(strategyJson))
        {
            var trimmedStrategy = JsonFieldSelector.KeepFields(strategyJson, StrategyFieldsNeeded);
            var wrappedStrategy = UntrustedTextSanitizer.Wrap(
                "Approved strategy JSON", [PiiRedactor.Redact(trimmedStrategy)], StrategyMaxLength);

            if (wrappedStrategy.Length > 0)
            {
                lines.Add(
                    "This campaign already has a marketing strategy that the business owner reviewed and approved. " +
                    "These posts are the execution of that strategy, so follow it: draw the themes from its " +
                    "campaignBlueprint.pillars and keyThemes, respect its contentMix when choosing each post's " +
                    "contentType, and keep the voice consistent with its brandStrategy and marketingStrategy.");
                lines.Add(wrappedStrategy);
            }
        }

        if (!string.IsNullOrWhiteSpace(briefJson))
        {
            var wrappedBrief = UntrustedTextSanitizer.Wrap(
                "Onboarding brief JSON, typed by the business", [PiiRedactor.Redact(briefJson)], BriefMaxLength);

            if (wrappedBrief.Length > 0)
            {
                lines.Add(
                    "The business's own onboarding answers, including their replies to AI follow-up questions — use " +
                    "these for audience and tone specifics:");
                lines.Add(wrappedBrief);
            }
        }

        // Competitor intelligence is deliberately not resent here: by the time a strategy is approved,
        // StrategyPrompt.BuildBlueprint has already absorbed the relevant competitor positioning into
        // campaignBlueprint (which strategyJson above carries trimmed) — a separate raw-excerpts block
        // was redundant grounding at real token cost, not new information for this stage to act on.

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

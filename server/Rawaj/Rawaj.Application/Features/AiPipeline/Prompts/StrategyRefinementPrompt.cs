using Rawaj.Application.Common.Services;
using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.Entities.Tenants;

namespace Rawaj.Application.Features.AiPipeline.Prompts;

/// <summary>
/// Refines an already-assembled strategy by free-text feedback, mirroring how a single content item
/// is revised — the model is asked to keep the same JSON shape so the approval UI keeps rendering it
/// unchanged.
///
/// <para><b>Prompt injection containment.</b> <c>feedback</c> is free text the tenant just typed into a
/// box for this exact request — the most directly tenant-authored input in the whole pipeline. Guard
/// sentence, PII redaction, and wrapping, same as the rest.</para>
///
/// <para><b>Grounding.</b> Also carries <see cref="MarketingCampaign.BriefJson"/> (the onboarding
/// answers) and <see cref="PromptFragments.RawajCapabilitiesInstruction"/>, same as
/// <c>ContentPlanPrompt</c> and <c>StrategyPrompt</c> — without them a refine pass only sees the
/// already-summarised strategy JSON and re-asks for answers the business already gave, or invents
/// capabilities (e.g. video) the platform doesn't have, since nothing here tells it otherwise.</para>
/// </summary>
public static class StrategyRefinementPrompt
{
    private const int StrategyMaxLength = 2_000;
    private const int BriefMaxLength = 1_500;

    public static string Build(TenantBrandProfile brand, MarketingCampaign campaign, string currentStrategyJson, string feedback)
    {
        var lines = new List<string>
        {
            PromptFragments.InjectionGuardInstruction,
            $"Revise the following marketing strategy for the campaign \"{campaign.Name}\" for the brand \"{brand.Name}\"."
        };

        var wrappedStrategy = UntrustedTextSanitizer.Wrap(
            "Current strategy JSON", [PiiRedactor.Redact(currentStrategyJson)], StrategyMaxLength);

        if (wrappedStrategy.Length > 0)
        {
            lines.Add(wrappedStrategy);
        }

        if (!string.IsNullOrWhiteSpace(campaign.BriefJson))
        {
            var wrappedBrief = UntrustedTextSanitizer.Wrap(
                "Onboarding brief JSON, typed by the business", [PiiRedactor.Redact(campaign.BriefJson)], BriefMaxLength);

            if (wrappedBrief.Length > 0)
            {
                lines.Add(
                    "The business's own onboarding answers — already known, do not ask for this again:");
                lines.Add(wrappedBrief);
            }
        }

        lines.Add(PromptFragments.RawajCapabilitiesInstruction);

        var wrappedFeedback = UntrustedTextSanitizer.Wrap(
            "Requested changes, typed by the business", [PiiRedactor.Redact(feedback)]);

        if (wrappedFeedback.Length > 0)
        {
            lines.Add(wrappedFeedback);
        }

        lines.Add("Keep everything that wasn't asked to change, and apply the requested changes precisely.");
        lines.Add(PromptFragments.ArabicOnlyInstruction);
        lines.Add(
            "Respond with ONLY a valid JSON object (no markdown fences, no commentary) using the exact same shape as the current strategy JSON above.");

        return string.Join(" ", lines);
    }
}

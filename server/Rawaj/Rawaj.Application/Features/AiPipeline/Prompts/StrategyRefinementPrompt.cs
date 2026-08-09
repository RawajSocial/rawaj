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
/// </summary>
public static class StrategyRefinementPrompt
{
    private const int StrategyMaxLength = 4_000;

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

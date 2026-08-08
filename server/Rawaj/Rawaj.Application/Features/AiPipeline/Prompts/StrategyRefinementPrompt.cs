using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.Entities.Tenants;

namespace Rawaj.Application.Features.AiPipeline.Prompts;

/// <summary>
/// Refines an already-assembled strategy by free-text feedback, mirroring how a single content item
/// is revised — the model is asked to keep the same JSON shape so the approval UI keeps rendering it
/// unchanged.
/// </summary>
public static class StrategyRefinementPrompt
{
    public static string Build(TenantBrandProfile brand, MarketingCampaign campaign, string currentStrategyJson, string feedback)
    {
        var lines = new List<string>
        {
            $"Revise the following marketing strategy for the campaign \"{campaign.Name}\" for the brand \"{brand.Name}\".",
            $"Current strategy JSON: {currentStrategyJson}",
            $"Requested changes: {feedback}",
            "Keep everything that wasn't asked to change, and apply the requested changes precisely.",
            PromptFragments.ArabicOnlyInstruction,
            "Respond with ONLY a valid JSON object (no markdown fences, no commentary) using the exact same shape as the current strategy JSON above."
        };

        return string.Join(" ", lines);
    }
}

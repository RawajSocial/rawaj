using Rawaj.Application.Common.Services;
using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.Entities.Tenants;

namespace Rawaj.Application.Features.AiPipeline.Prompts;

/// <summary>
/// The <c>CampaignAnalysis</c> stage's prompt: what this particular campaign is trying to achieve,
/// for whom, and under what constraints.
///
/// <para>It also produces <c>researchQueries</c>, which the two research stages then execute. That is
/// a deliberate change of approach: today the competitor search string is assembled by concatenating
/// the brand name, its industry field and its keywords, so research quality is a function of how
/// carefully someone filled in a form. Letting the model that has just read the whole brief choose
/// what to look up is the point of having an analysis stage at all.</para>
///
/// <para><b>Prompt injection containment.</b> Same treatment as <c>BrandAnalysisPrompt</c>: a
/// standalone guard sentence, every tenant-authored free-text field (campaign objective, onboarding
/// brief) wrapped via <see cref="UntrustedTextSanitizer"/> and PII-redacted first. <c>brandAnalysisJson</c>
/// is wrapped too even though its own inputs were already contained upstream — that stage's LLM call
/// could still launder an injected instruction into its output text, so this is defense-in-depth on
/// top of, not instead of, containing it at the source.</para>
/// </summary>
public static class CampaignAnalysisPrompt
{
    /// <summary>Cap for the brand-analysis JSON block — larger than a single free-text field since
    /// it's one whole synthesized artifact (summary, SWOT, guardrails, etc.), not competing spans.</summary>
    private const int BrandAnalysisMaxLength = 4_000;

    private const int BriefMaxLength = 3_000;

    public static string Build(
        TenantBrandProfile brand, MarketingCampaign campaign, string? briefJson, string? brandAnalysisJson)
    {
        var lines = new List<string> { PromptFragments.InjectionGuardInstruction };

        lines.Add(
            $"You are a marketing strategist analysing the campaign \"{campaign.Name}\" for the brand \"{brand.Name}\" " +
            "before any strategy is written for it.");

        if (!string.IsNullOrWhiteSpace(brandAnalysisJson))
        {
            var wrappedAnalysis = UntrustedTextSanitizer.Wrap(
                "Durable brand analysis, already produced", [brandAnalysisJson], BrandAnalysisMaxLength);

            if (wrappedAnalysis.Length > 0)
            {
                lines.Add("A durable analysis of this brand already exists — treat it as established and do not restate it.");
                lines.Add(wrappedAnalysis);
            }
        }
        else
        {
            // Only reachable if brand analysis was somehow unavailable; without it the model would
            // otherwise have no idea who the campaign is for. Wrapped, same as the primary path above —
            // these are the tenant's own raw fields, not yet contained by an upstream analysis stage.
            PromptFragments.AddWrappedBrandIdentity(lines, brand);
        }

        if (!string.IsNullOrWhiteSpace(campaign.Objective))
        {
            var wrappedObjective = UntrustedTextSanitizer.Wrap(
                "Campaign objective, typed by the business", [PiiRedactor.Redact(campaign.Objective)]);

            if (wrappedObjective.Length > 0)
            {
                lines.Add(wrappedObjective);
            }
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
            var wrappedBrief = UntrustedTextSanitizer.Wrap(
                "Onboarding brief JSON, typed by the business", [PiiRedactor.Redact(briefJson)], BriefMaxLength);

            if (wrappedBrief.Length > 0)
            {
                lines.Add("The business's own onboarding answers, including their replies to AI follow-up questions:");
                lines.Add(wrappedBrief);
            }
        }

        lines.Add(
            "Write the human-readable fields in Arabic: classify what this campaign is really trying to achieve " +
            "(which may be more specific than what the user wrote), describe the distinct audience segments it should " +
            "address with their traits and motivations, the criteria by which it should be judged a success, the " +
            "constraints it must work within (budget, duration, platform, capability), and the risks and " +
            "opportunities specific to this campaign.");

        lines.Add(
            "Then propose web search queries that would genuinely help: under \"market\", queries about the industry, " +
            "demand, trends and seasonality; under \"competitor\", queries that would surface the specific businesses " +
            "competing with this one. Give 2 to 4 of each. Write each query in whichever language is most likely to " +
            "surface useful results — Arabic for local and regional searches, English for global or industry ones — " +
            "and make them specific enough to return this business's actual market rather than generic articles.");

        lines.Add(PromptFragments.JsonObjectOnly(
            "{\"objectiveClassification\":\"...\"," +
            "\"audienceSegments\":[{\"name\":\"...\",\"traits\":[\"...\"],\"motivations\":[\"...\"]}]," +
            "\"successCriteria\":[\"...\"],\"constraints\":[\"...\"],\"risks\":[\"...\"],\"opportunities\":[\"...\"]," +
            "\"researchQueries\":{\"market\":[\"...\"],\"competitor\":[\"...\"]}}"));

        return string.Join(" ", lines);
    }
}

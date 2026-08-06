using Rawaj.Domain.Entities.Tenants;

namespace Rawaj.Application.Features.AiPipeline.Prompts;

/// <summary>
/// The <c>BrandAnalysis</c> stage's prompt: a durable reading of who the brand is, computed once per
/// brand and reused by every campaign it runs.
///
/// <para>This replaces something that currently happens implicitly and repeatedly. The brand's raw
/// identity fields are pasted into every prompt of every campaign and re-interpreted by the model
/// each time — paid for on every call, and inconsistent between them in a way a brand voice
/// especially should not be. Deriving it once and passing the result down is cheaper and steadier.</para>
///
/// <para>The narrative fields are Arabic because they surface in the strategy review the user reads;
/// the instructions stay English, which is the language the model follows best.</para>
/// </summary>
public static class BrandAnalysisPrompt
{
    public static string Build(TenantBrandProfile brand, string? briefJson)
    {
        var lines = new List<string>
        {
            $"You are a brand strategist producing a durable profile of the brand \"{brand.Name}\". " +
            "This profile will be reused across every future marketing campaign for this brand, so describe what is " +
            "true of the brand itself rather than of any one campaign."
        };

        PromptFragments.AddBrandIdentity(lines, brand);

        if (!string.IsNullOrWhiteSpace(briefJson))
        {
            lines.Add(
                "Here is the JSON of what the business entered during onboarding, including their answers to AI " +
                "follow-up questions. Use it for anything the brand fields above do not cover: " + briefJson);
        }

        lines.Add(
            "Write the human-readable fields in Arabic: a plain-language summary of what this business is and does, " +
            "its core identity in one or two sentences, its value propositions, and what genuinely differentiates it " +
            "from similar businesses. Also give a SWOT analysis, how mature the business is, and how ready its " +
            "marketing currently is.");

        lines.Add(
            "For voiceProfile, describe the tone and register its content should use, and list things its content " +
            "should avoid saying or doing. For contentGuardrails, list rules any future post for this brand must " +
            "respect — claims it must not make, topics to stay away from, regulatory or cultural sensitivities.");

        lines.Add(
            "Base everything on what you were actually told. Where something important is missing, say so in " +
            "missingInformation rather than inventing it.");

        lines.Add(PromptFragments.JsonObjectOnly(
            "{\"businessSummary\":\"...\",\"identityCore\":\"...\"," +
            "\"voiceProfile\":{\"tone\":\"...\",\"register\":\"...\",\"avoid\":[\"...\"]}," +
            "\"valuePropositions\":[\"...\"],\"differentiators\":[\"...\"],\"contentGuardrails\":[\"...\"]," +
            "\"swot\":{\"strengths\":[\"...\"],\"weaknesses\":[\"...\"],\"opportunities\":[\"...\"],\"threats\":[\"...\"]}," +
            "\"businessMaturity\":\"...\",\"marketingReadiness\":\"...\",\"missingInformation\":[\"...\"]}"));

        return string.Join(" ", lines);
    }
}

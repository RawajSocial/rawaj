using Rawaj.Application.Common.Services;
using Rawaj.Domain.Entities.Tenants;

namespace Rawaj.Application.Features.AiPipeline.Prompts;

/// <summary>
/// Turns raw web search results into a research artifact.
///
/// <para>This is the only place in the pipeline where attacker-controllable text reaches a prompt.
/// Search result bodies are written by whoever owns the page, and the eventual output of this chain
/// is published to a customer's real social accounts — so the excerpts go in wrapped by
/// <see cref="UntrustedTextSanitizer"/>, and everything downstream reads this stage's synthesised
/// artifact rather than the raw text.</para>
/// </summary>
public static class ResearchSynthesisPrompt
{
    public static string BuildMarket(
        TenantBrandProfile brand, IReadOnlyList<string> queries, IReadOnlyList<string> excerpts, string? searchAnswer)
    {
        var lines = new List<string>
        {
            $"You are a market analyst studying the market that the brand \"{brand.Name}\" operates in."
        };

        if (!string.IsNullOrWhiteSpace(brand.BrandInfo?.Industry))
        {
            lines.Add($"Industry: {brand.BrandInfo.Industry}.");
        }

        if (!string.IsNullOrWhiteSpace(brand.BrandInfo?.Location))
        {
            lines.Add($"Market location: {brand.BrandInfo.Location}.");
        }

        lines.Add($"These searches were run: {string.Join("; ", queries)}.");

        if (!string.IsNullOrWhiteSpace(searchAnswer))
        {
            lines.Add(UntrustedTextSanitizer.Wrap("Search engine summary", [searchAnswer]));
        }

        lines.Add(UntrustedTextSanitizer.Wrap("Web page excerpts", excerpts));

        lines.Add(PromptFragments.ArabicOnlyInstruction);
        lines.Add("Exception: sources[].title must be copied from the source page as-is, in its original language.");

        lines.Add(
            "From that material, write: the trends actually visible in this market, the demand signals you " +
            "can support with what you read, any seasonality that matters for marketing timing, and per-platform " +
            "benchmarks or norms where the sources mention them.");

        lines.Add(
            "Report only what the sources support. If they are thin or off-topic, say so in \"note\" and return fewer " +
            "items rather than filling the gaps with general knowledge — a confident summary of nothing is worse than " +
            "an honest empty one.");

        lines.Add(PromptFragments.JsonObjectOnly(
            "{\"trends\":[\"...\"],\"demandSignals\":[\"...\"],\"seasonality\":\"...\"," +
            "\"platformBenchmarks\":[{\"platform\":\"...\",\"note\":\"...\"}]," +
            "\"sources\":[{\"title\":\"...\",\"url\":\"...\"}],\"unavailable\":false,\"note\":null}"));

        return string.Join(" ", lines);
    }

    public static string BuildCompetitor(
        TenantBrandProfile brand, IReadOnlyList<string> queries, IReadOnlyList<string> excerpts, string? searchAnswer)
    {
        var lines = new List<string>
        {
            $"You are a competitive analyst identifying and assessing the businesses competing with \"{brand.Name}\"."
        };

        if (!string.IsNullOrWhiteSpace(brand.BrandInfo?.Industry))
        {
            lines.Add($"Industry: {brand.BrandInfo.Industry}.");
        }

        if (!string.IsNullOrWhiteSpace(brand.BrandInfo?.Location))
        {
            lines.Add($"Market location: {brand.BrandInfo.Location}.");
        }

        lines.Add($"These searches were run: {string.Join("; ", queries)}.");

        if (!string.IsNullOrWhiteSpace(searchAnswer))
        {
            lines.Add(UntrustedTextSanitizer.Wrap("Search engine summary", [searchAnswer]));
        }

        lines.Add(UntrustedTextSanitizer.Wrap("Web page excerpts", excerpts));

        lines.Add(PromptFragments.ArabicOnlyInstruction);
        lines.Add("Exception: sources[].title must be copied from the source page as-is, in its original language.");

        lines.Add(
            "From that material, write: the competitors you can actually identify with a one-line note on " +
            "each, how they position themselves relative to one another, the content patterns they appear to follow, " +
            "and the gaps none of them are covering — the gaps are the most useful part, so be specific.");

        lines.Add(
            "Include only businesses genuinely competing with this one. Directories, news articles and unrelated " +
            "companies that happen to appear in the results are not competitors; leave them out rather than padding " +
            "the list.");

        lines.Add(PromptFragments.JsonObjectOnly(
            "{\"competitors\":[{\"name\":\"...\",\"url\":\"...\",\"snippet\":\"...\"}],\"positioningMap\":\"...\"," +
            "\"contentPatterns\":[\"...\"],\"gaps\":[\"...\"]," +
            "\"sources\":[{\"title\":\"...\",\"url\":\"...\"}],\"unavailable\":false,\"note\":null}"));

        return string.Join(" ", lines);
    }
}

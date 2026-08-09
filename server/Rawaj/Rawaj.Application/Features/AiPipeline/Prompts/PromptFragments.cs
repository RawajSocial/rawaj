using Rawaj.Domain.Entities.Tenants;

namespace Rawaj.Application.Features.AiPipeline.Prompts;

/// <summary>
/// The prompt pieces shared across stages, extracted from <c>ContentPromptBuilder</c> so per-stage
/// prompt files can use them without every stage's prompt living in one 457-line class — which is
/// also what lets the Phase 4 stage commits be independent instead of five edits to the same file.
///
/// <para>Language strategy, preserved from the original: instructions are written in English because
/// instruction-following is stronger in it, while the deliverable's language is stated explicitly so
/// the output stays Arabic-first.</para>
/// </summary>
public static class PromptFragments
{
    /// <summary>
    /// Closing instruction on every image prompt. Diffusion models render text badly at the best of
    /// times and near-unreadably in Arabic script, so a prompt carrying Arabic marketing copy used
    /// to come back with mangled pseudo-Arabic lettering baked into the picture. Asking for a clean
    /// image with no lettering, and stating the prompt language explicitly, is what keeps the output
    /// usable — the caption stays where it belongs, next to the image, not inside it.
    /// </summary>
    public const string NoRenderedTextInstruction =
        "Render a clean photographic or illustrative image with no words, letters, captions, logos or watermarks " +
        "anywhere in it. Interpret this prompt as English.";

    /// <summary>
    /// Added to a retry after the previous attempt returned something unparseable. Offered once only
    /// (see <c>AiPipelinePolicy.ShouldRepairPrompt</c>) — a model that ignores the schema twice will
    /// not be argued into it on a third paid call.
    /// </summary>
    public const string RepairInstruction =
        "Your previous response could not be parsed as JSON. Return ONLY the raw JSON object, starting with { and " +
        "ending with }, with no markdown fences, no explanation and no text before or after it.";

    /// <summary>
    /// The Arabic-language rule itself. Earlier prompts stated this as a single soft mid-sentence
    /// clause ("write ... in Arabic"), which a model can drift away from once enough non-Arabic text
    /// (source excerpts, brand fields typed in English, a long English-language instruction block)
    /// sits nearby in the same prompt. This is deliberately a standalone, negatively-constrained
    /// sentence so it can be dropped in verbatim near the top of a prompt (before the model reads the
    /// task) and again near the JSON-shape closer, rather than relying on one mention to survive the
    /// whole prompt.
    /// </summary>
    public const string ArabicOnlyInstruction =
        "Write your entire response in Modern Standard Arabic. Do not use English, Chinese, or any other " +
        "language or script anywhere in your output — including in labels, names, or examples you introduce yourself.";

    /// <summary>
    /// Added to a retry after the previous attempt's output failed <c>ArabicContentPolicy</c>'s
    /// check — same single-retry-then-fail shape as <see cref="RepairInstruction"/>, but for a
    /// response that parsed fine yet came back in the wrong language.
    /// </summary>
    public const string LanguageRepairInstruction =
        "Your previous response used a language other than Arabic. Rewrite it entirely in Modern Standard " +
        "Arabic only, with no English, Chinese, or any other language or script anywhere in it.";

    /// <summary>
    /// The brand identity block. Every line is conditional: an unset field is omitted rather than
    /// sent as an empty label, which would read to the model as "this brand has no industry".
    /// </summary>
    public static void AddBrandIdentity(List<string> lines, TenantBrandProfile brand)
    {
        if (!string.IsNullOrWhiteSpace(brand.Description))
        {
            lines.Add($"Brand description: {brand.Description}.");
        }

        if (!string.IsNullOrWhiteSpace(brand.BrandInfo?.Tagline))
        {
            lines.Add($"Brand tagline: {brand.BrandInfo.Tagline}.");
        }

        if (!string.IsNullOrWhiteSpace(brand.BrandInfo?.Industry))
        {
            lines.Add($"Industry: {brand.BrandInfo.Industry}.");
        }

        if (!string.IsNullOrWhiteSpace(brand.BrandInfo?.TargetAudience))
        {
            lines.Add($"Target audience: {brand.BrandInfo.TargetAudience}.");
        }

        if (brand.BrandInfo?.Keywords is { Count: > 0 })
        {
            lines.Add($"Relevant keywords: {string.Join(", ", brand.BrandInfo.Keywords)}.");
        }

        if (brand.BrandInfo?.Tones is { Count: > 0 })
        {
            lines.Add($"Brand voice/tone: {string.Join(", ", brand.BrandInfo.Tones)}.");
        }

        if (!string.IsNullOrWhiteSpace(brand.BrandInfo?.UniqueValue))
        {
            lines.Add($"Unique value proposition: {brand.BrandInfo.UniqueValue}.");
        }

        if (!string.IsNullOrWhiteSpace(brand.BrandInfo?.PricePositioning))
        {
            lines.Add($"Price positioning: {brand.BrandInfo.PricePositioning}.");
        }
    }

    /// <summary>
    /// The "give me only JSON" closer. Structured output is requested, not enforced — Groq's JSON
    /// mode is wired in separately (and even then a parser fallback remains), because a fenced
    /// response stored as a strategy has already produced a blank page for a user who paid 12,000
    /// coins for it.
    /// </summary>
    public static string JsonObjectOnly(string shape) =>
        "Respond with ONLY a valid JSON object (no markdown fences, no commentary) with this exact shape: " + shape;

    public static string JsonArrayOnly(string shape) =>
        "Respond with ONLY a valid JSON array (no markdown fences, no commentary) with this exact shape: " + shape;
}

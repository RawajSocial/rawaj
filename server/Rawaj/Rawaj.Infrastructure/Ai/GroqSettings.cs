namespace Rawaj.Infrastructure.Ai;

/// <summary>
/// Groq's OpenAI-compatible chat completions API - used as the text generation provider.
/// </summary>
public class GroqSettings
{
    public const string SectionName = "Groq";

    /// <summary>
    /// Tried in order, falling through to the next on quota/auth rejection. Groq enforces its
    /// tokens-per-day cap per *organization*, not per key, so a second key only buys extra
    /// headroom when it belongs to a different Groq account - two keys from the same org share
    /// (and exhaust) the same daily budget.
    /// </summary>
    public List<string> ApiKeys { get; set; } = [];

    /// <summary>Used for any task with no entry in <see cref="Models"/>.</summary>
    public string Model { get; set; } = "llama-3.3-70b-versatile";

    /// <summary>
    /// Per-task model overrides, keyed by the task name on
    /// <c>AiTextGenerationOptions.TaskName</c> (e.g. "Strategy", "Content", "BrandAnalysis").
    ///
    /// <para>Empty by default, so every task keeps using <see cref="Model"/> until someone decides
    /// otherwise. The point is that the decision becomes configuration rather than code: the same
    /// model currently writes the throwaway onboarding questions and the most valuable artifact in
    /// the product, and there is no way to raise quality where it pays without raising cost
    /// everywhere.</para>
    /// </summary>
    public Dictionary<string, string> Models { get; set; } = [];

    public string BaseUrl { get; set; } = "https://api.groq.com/openai/v1";
    public int MaxTokens { get; set; } = 4000;

    /// <summary>
    /// Whether to send <c>response_format: {"type":"json_object"}</c> when a caller asks for JSON
    /// mode. A kill switch, not a preference — if a future model or a Groq-compatible endpoint
    /// rejects the parameter, this turns it off without a redeploy, and the JSON parser that has
    /// always run behind it keeps working.
    /// </summary>
    public bool EnableJsonMode { get; set; } = true;

    public string ResolveModel(string? taskName) =>
        taskName is not null && Models.TryGetValue(taskName, out var model) && !string.IsNullOrWhiteSpace(model)
            ? model
            : Model;
}

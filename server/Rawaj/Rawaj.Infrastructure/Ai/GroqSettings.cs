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
    public string Model { get; set; } = "llama-3.3-70b-versatile";
    public string BaseUrl { get; set; } = "https://api.groq.com/openai/v1";
    public int MaxTokens { get; set; } = 4000;
}

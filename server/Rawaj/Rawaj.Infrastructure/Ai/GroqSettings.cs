namespace Rawaj.Infrastructure.Ai;

/// <summary>
/// Groq's OpenAI-compatible chat completions API - used as the text generation provider.
/// </summary>
public class GroqSettings
{
    public const string SectionName = "Groq";

    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "llama-3.3-70b-versatile";
    public string BaseUrl { get; set; } = "https://api.groq.com/openai/v1";
    public int MaxTokens { get; set; } = 4000;
}

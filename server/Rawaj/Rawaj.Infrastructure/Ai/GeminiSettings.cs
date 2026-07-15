namespace Rawaj.Infrastructure.Ai;

public class GeminiSettings
{
    public const string SectionName = "Gemini";

    public List<string> ApiKeys { get; set; } = [];
    public string Model { get; set; } = "gemini-2.0-flash";
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta/models/";
}

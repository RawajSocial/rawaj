namespace Rawaj.Infrastructure.Ai;

public class TavilySettings
{
    public const string SectionName = "Tavily";

    public List<string> ApiKeys { get; set; } = [];
    public string BaseUrl { get; set; } = "https://api.tavily.com/";
    public int MaxResults { get; set; } = 5;
}

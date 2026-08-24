namespace Rawaj.Application.Common.Models;

public record TavilySearchItem(string Title, string Url, string Content);

public class TavilySearchResult
{
    public bool Succeeded { get; }
    public string? Answer { get; }
    public List<TavilySearchItem> Results { get; }
    public string? ErrorMessage { get; }

    private TavilySearchResult(bool succeeded, string? answer, List<TavilySearchItem> results, string? errorMessage)
    {
        Succeeded = succeeded;
        Answer = answer;
        Results = results;
        ErrorMessage = errorMessage;
    }

    public static TavilySearchResult Success(string? answer, List<TavilySearchItem> results) =>
        new(true, answer, results, null);

    public static TavilySearchResult Failure(string errorMessage) => new(false, null, [], errorMessage);
}

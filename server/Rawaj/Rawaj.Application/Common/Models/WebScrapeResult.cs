namespace Rawaj.Application.Common.Models;

public class WebScrapeResult
{
    public bool Succeeded { get; }
    public string? Title { get; }
    public string? TextContent { get; }
    public string? ErrorMessage { get; }

    private WebScrapeResult(bool succeeded, string? title, string? textContent, string? errorMessage)
    {
        Succeeded = succeeded;
        Title = title;
        TextContent = textContent;
        ErrorMessage = errorMessage;
    }

    public static WebScrapeResult Success(string? title, string textContent) => new(true, title, textContent, null);

    public static WebScrapeResult Failure(string errorMessage) => new(false, null, null, errorMessage);
}

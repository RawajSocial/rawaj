using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;

namespace Rawaj.Infrastructure.Scraping;

public class HtmlAgilityPackWebScraperService(
    IHttpClientFactory httpClientFactory, ILogger<HtmlAgilityPackWebScraperService> logger) : IWebScraperService
{
    private const int MaxTextLength = 20_000;
    private static readonly string[] NoiseNodeNames = ["script", "style", "nav", "footer", "header", "noscript", "svg", "iframe"];

    public async Task<WebScrapeResult> ScrapeAsync(string url, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return WebScrapeResult.Failure("Only absolute http(s) URLs can be scraped.");
        }

        var client = httpClientFactory.CreateClient("WebScraper");

        try
        {
            using var response = await client.GetAsync(uri, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return WebScrapeResult.Failure($"Fetching the page failed with status {(int)response.StatusCode}.");
            }

            var html = await response.Content.ReadAsStringAsync(cancellationToken);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            foreach (var noisyNode in doc.DocumentNode
                         .Descendants()
                         .Where(n => NoiseNodeNames.Contains(n.Name, StringComparer.OrdinalIgnoreCase))
                         .ToList())
            {
                noisyNode.Remove();
            }

            var title = doc.DocumentNode.SelectSingleNode("//title")?.InnerText.Trim();

            var text = HtmlEntity.DeEntitize(doc.DocumentNode.InnerText);
            var normalized = string.Join(
                ' ', text.Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries));

            if (string.IsNullOrWhiteSpace(normalized))
            {
                return WebScrapeResult.Failure("No readable text content was found on the page.");
            }

            var truncated = normalized.Length > MaxTextLength ? normalized[..MaxTextLength] : normalized;

            return WebScrapeResult.Success(title, truncated);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Scraping {Url} threw an exception.", url);
            return WebScrapeResult.Failure(ex.Message);
        }
    }
}

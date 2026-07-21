using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Polly.Timeout;
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
        catch (HttpRequestException ex)
        {
            // Covers connection resets, SSL handshake failures, DNS failures, etc. - most commonly
            // a site's bot-protection (WAF/CDN) closing the connection because this scraper
            // truthfully identifies as a bot rather than spoofing a browser. The raw exception
            // message (socket/TLS internals) is not something a business user can act on.
            logger.LogWarning(ex, "Scraping {Url} failed to connect.", url);
            return WebScrapeResult.Failure(
                "Could not reach this website. It may be blocking automated visits, or it's temporarily unreachable.");
        }
        catch (TimeoutRejectedException ex)
        {
            // The resilience handler's per-attempt or total-request timeout tripped (e.g. the site
            // returned a transient 5xx, got retried, and the retries together ran past the total
            // timeout) - this is Polly's own exception type, not a .NET cancellation/timeout type.
            logger.LogWarning(ex, "Scraping {Url} timed out.", url);
            return WebScrapeResult.Failure("This website took too long to respond, or is temporarily unavailable.");
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            // A plain (non-Polly) timeout can still surface as TaskCanceledException, distinct
            // from the caller's own cancellationToken firing (excluded via the when-clause).
            logger.LogWarning(ex, "Scraping {Url} timed out.", url);
            return WebScrapeResult.Failure("This website took too long to respond.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Scraping {Url} threw an exception.", url);
            return WebScrapeResult.Failure("Could not read this website's content. Please try again.");
        }
    }
}

using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Common.Interfaces;

/// <summary>
/// Scrapes a specific, user-supplied URL directly - distinct from ITavilySearchService, which
/// runs a general web search and returns whatever pages the search engine picks. Use this when
/// the caller already knows exactly which page they want indexed (e.g. a competitor's own
/// pricing or about page).
/// </summary>
public interface IWebScraperService
{
    Task<WebScrapeResult> ScrapeAsync(string url, CancellationToken cancellationToken);
}

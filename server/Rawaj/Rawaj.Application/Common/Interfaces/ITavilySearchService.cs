using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Common.Interfaces;

public interface ITavilySearchService
{
    Task<TavilySearchResult> SearchAsync(string query, CancellationToken cancellationToken);
}

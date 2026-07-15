using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;

namespace Rawaj.Infrastructure.Ai;

public class TavilySearchService(
    IHttpClientFactory httpClientFactory,
    IOptions<TavilySettings> settings,
    ILogger<TavilySearchService> logger) : ITavilySearchService
{
    private readonly TavilySettings _settings = settings.Value;

    public async Task<TavilySearchResult> SearchAsync(string query, CancellationToken cancellationToken)
    {
        if (_settings.ApiKeys.Count == 0)
        {
            return TavilySearchResult.Failure("No Tavily API key is configured.");
        }

        string? lastError = null;

        foreach (var apiKey in _settings.ApiKeys)
        {
            var client = httpClientFactory.CreateClient("Tavily");
            client.BaseAddress = new Uri(_settings.BaseUrl);

            var request = new TavilySearchRequest(
                apiKey,
                query,
                "basic",
                true,
                _settings.MaxResults);

            try
            {
                using var response = await client.PostAsJsonAsync("search", request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    lastError = $"Tavily request failed with status {(int)response.StatusCode}.";
                    logger.LogWarning("Tavily key ending in {KeySuffix} failed with {StatusCode}, trying next key.",
                        apiKey[^Math.Min(4, apiKey.Length)..], response.StatusCode);
                    continue;
                }

                var payload = await response.Content.ReadFromJsonAsync<TavilySearchResponse>(cancellationToken: cancellationToken);
                if (payload is null)
                {
                    lastError = "Tavily returned an empty response.";
                    continue;
                }

                var results = (payload.Results ?? [])
                    .Select(r => new TavilySearchItem(r.Title, r.Url, r.Content))
                    .ToList();

                return TavilySearchResult.Success(payload.Answer, results);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                lastError = ex.Message;
                logger.LogWarning(ex, "Tavily request threw an exception, trying next key if available.");
            }
        }

        return TavilySearchResult.Failure(lastError ?? "Competitor search failed.");
    }

    private record TavilySearchRequest(
        [property: JsonPropertyName("api_key")] string ApiKey,
        string Query,
        [property: JsonPropertyName("search_depth")] string SearchDepth,
        [property: JsonPropertyName("include_answer")] bool IncludeAnswer,
        [property: JsonPropertyName("max_results")] int MaxResults);

    private record TavilySearchResponse(string? Answer, List<TavilySearchResultItem>? Results);

    private record TavilySearchResultItem(string Title, string Url, string Content);
}

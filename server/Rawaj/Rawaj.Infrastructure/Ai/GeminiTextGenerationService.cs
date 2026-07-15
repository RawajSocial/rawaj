using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;

namespace Rawaj.Infrastructure.Ai;

public class GeminiTextGenerationService(
    IHttpClientFactory httpClientFactory,
    IOptions<GeminiSettings> settings,
    ILogger<GeminiTextGenerationService> logger) : IAiTextGenerationService
{
    private readonly GeminiSettings _settings = settings.Value;

    public async Task<AiTextGenerationResult> GenerateTextAsync(string prompt, CancellationToken cancellationToken)
    {
        if (_settings.ApiKeys.Count == 0)
        {
            return AiTextGenerationResult.Failure("No Gemini API key is configured.");
        }

        string? lastError = null;

        foreach (var apiKey in _settings.ApiKeys)
        {
            var client = httpClientFactory.CreateClient("Gemini");
            client.BaseAddress = new Uri(_settings.BaseUrl);

            var request = new GeminiGenerateRequest([new GeminiContent([new GeminiPart(prompt)])]);
            var requestUri = $"./{_settings.Model}:generateContent?key={apiKey}";

            try
            {
                using var response = await client.PostAsJsonAsync(requestUri, request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    lastError = $"Gemini request failed with status {(int)response.StatusCode}.";
                    logger.LogWarning("Gemini key ending in {KeySuffix} failed with {StatusCode}, trying next key.",
                        apiKey[^Math.Min(4, apiKey.Length)..], response.StatusCode);
                    continue;
                }

                var payload = await response.Content.ReadFromJsonAsync<GeminiGenerateResponse>(cancellationToken: cancellationToken);
                var text = payload?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

                if (string.IsNullOrWhiteSpace(text))
                {
                    lastError = "Gemini returned an empty response.";
                    continue;
                }

                return AiTextGenerationResult.Success(text.Trim(), payload?.UsageMetadata?.TotalTokenCount);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                lastError = ex.Message;
                logger.LogWarning(ex, "Gemini request threw an exception, trying next key if available.");
            }
        }

        return AiTextGenerationResult.Failure(lastError ?? "Text generation failed.");
    }

    private record GeminiGenerateRequest(List<GeminiContent> Contents);

    private record GeminiContent(List<GeminiPart> Parts);

    private record GeminiPart(string Text);

    private record GeminiGenerateResponse(
        List<GeminiCandidate>? Candidates,
        [property: JsonPropertyName("usageMetadata")] GeminiUsageMetadata? UsageMetadata);

    private record GeminiCandidate(GeminiContent? Content);

    private record GeminiUsageMetadata([property: JsonPropertyName("totalTokenCount")] int TotalTokenCount);
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;

namespace Rawaj.Infrastructure.Ai;

public class GroqTextGenerationService(
    IHttpClientFactory httpClientFactory,
    IOptions<GroqSettings> settings,
    ILogger<GroqTextGenerationService> logger) : IAiTextGenerationService
{
    private readonly GroqSettings _settings = settings.Value;

    public async Task<AiTextGenerationResult> GenerateTextAsync(string prompt, CancellationToken cancellationToken)
    {
        var apiKeys = _settings.ApiKeys.Where(k => !string.IsNullOrWhiteSpace(k)).ToList();
        if (apiKeys.Count == 0)
        {
            return AiTextGenerationResult.Failure("No Groq API key is configured.");
        }

        var request = new GroqChatRequest(_settings.Model, [new GroqChatMessage("user", prompt)], _settings.MaxTokens);
        string? lastError = null;

        foreach (var apiKey in apiKeys)
        {
            var client = httpClientFactory.CreateClient("Groq");
            client.BaseAddress = new Uri(_settings.BaseUrl.TrimEnd('/') + "/");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            try
            {
                using var response = await client.PostAsJsonAsync("chat/completions", request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    string message;
                    try
                    {
                        var errorBody = await response.Content.ReadFromJsonAsync<GroqErrorResponse>(cancellationToken: cancellationToken);
                        message = errorBody?.Error?.Message ?? $"Groq request failed with status {(int)response.StatusCode}.";
                    }
                    catch (Exception)
                    {
                        message = $"Groq request failed with status {(int)response.StatusCode}.";
                    }

                    lastError = message;

                    // Only a quota/auth rejection is worth retrying on another key - that failure
                    // belongs to the key (or its organization), so a key from a different Groq
                    // account can still succeed. Anything else (a malformed prompt, a provider
                    // outage) would fail identically on every key, so it's surfaced immediately
                    // rather than burning the whole list on a doomed request.
                    if (IsKeyRejection(response.StatusCode))
                    {
                        logger.LogWarning(
                            "Groq key ending in {KeySuffix} rejected with {StatusCode}, trying next key if available: {Message}",
                            apiKey[^Math.Min(4, apiKey.Length)..], response.StatusCode, message);
                        continue;
                    }

                    logger.LogWarning("Groq chat request failed with {StatusCode}: {Message}", response.StatusCode, message);
                    return AiTextGenerationResult.Failure(message);
                }

                var payload = await response.Content.ReadFromJsonAsync<GroqChatResponse>(cancellationToken: cancellationToken);
                var outputText = payload?.Choices?.FirstOrDefault()?.Message?.Content;

                if (string.IsNullOrWhiteSpace(outputText))
                {
                    return AiTextGenerationResult.Failure("Groq returned an empty response.");
                }

                return AiTextGenerationResult.Success(outputText.Trim(), payload?.Usage?.TotalTokens);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                lastError = ex.Message;
                logger.LogWarning(ex, "Groq request threw an exception, trying next key if available.");
            }
        }

        return AiTextGenerationResult.Failure(lastError ?? "Text generation failed. Please try again.");
    }

    /// <summary>Statuses that mean "this key can't serve the request" - quota exhausted, revoked,
    /// or unpaid - as opposed to a fault that would repeat on every key.</summary>
    private static bool IsKeyRejection(HttpStatusCode status) =>
        status is HttpStatusCode.TooManyRequests
            or HttpStatusCode.Unauthorized
            or HttpStatusCode.Forbidden
            or HttpStatusCode.PaymentRequired;

    private record GroqChatRequest(
        string Model,
        List<GroqChatMessage> Messages,
        [property: JsonPropertyName("max_tokens")] int MaxTokens);

    private record GroqChatMessage(string Role, string Content);

    private record GroqChatResponse(List<GroqChatChoice>? Choices, GroqUsage? Usage);

    private record GroqChatChoice(GroqChatMessage? Message);

    private record GroqUsage([property: JsonPropertyName("total_tokens")] int TotalTokens);

    private record GroqErrorResponse(GroqErrorDetail? Error);

    private record GroqErrorDetail(string? Code, string? Message);
}

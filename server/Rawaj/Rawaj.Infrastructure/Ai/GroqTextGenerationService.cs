using System.Diagnostics;
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

    public async Task<AiTextGenerationResult> GenerateTextAsync(
        string prompt, CancellationToken cancellationToken, AiTextGenerationOptions? options = null)
    {
        options ??= AiTextGenerationOptions.Default;

        var apiKeys = _settings.ApiKeys.Where(k => !string.IsNullOrWhiteSpace(k)).ToList();
        if (apiKeys.Count == 0)
        {
            return AiTextGenerationResult.Failure("No Groq API key is configured.");
        }

        var model = _settings.ResolveModel(options.TaskName);

        // Groq's OpenAI-compatible JSON mode. It constrains syntax only — the response is still not
        // guaranteed to match the shape the prompt asked for — so AiJsonResponseParser stays in
        // place behind it rather than being replaced by it. That parser exists because a
        // fence-wrapped response was once stored as a strategy, passing every "a plan exists" check
        // while rendering as a blank page for a user who had paid 12,000 coins.
        var responseFormat = options.JsonMode && _settings.EnableJsonMode
            ? new GroqResponseFormat("json_object")
            : null;

        var request = new GroqChatRequest(
            model,
            [new GroqChatMessage("user", prompt)],
            _settings.MaxTokens,
            responseFormat,
            options.Temperature,
            options.Seed);

        string? lastError = null;
        var startedAt = Stopwatch.GetTimestamp();

        int ElapsedMs() => (int)Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;

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
                    return AiTextGenerationResult.Failure(message, model, ElapsedMs());
                }

                var payload = await response.Content.ReadFromJsonAsync<GroqChatResponse>(cancellationToken: cancellationToken);
                var outputText = payload?.Choices?.FirstOrDefault()?.Message?.Content;

                if (string.IsNullOrWhiteSpace(outputText))
                {
                    return AiTextGenerationResult.Failure("Groq returned an empty response.", model, ElapsedMs());
                }

                return AiTextGenerationResult.Success(
                    outputText.Trim(), payload?.Usage?.TotalTokens, model, ElapsedMs());
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                lastError = ex.Message;
                logger.LogWarning(ex, "Groq request threw an exception, trying next key if available.");
            }
        }

        return AiTextGenerationResult.Failure(
            lastError ?? "Text generation failed. Please try again.", model, ElapsedMs());
    }

    /// <summary>Statuses that mean "this key can't serve the request" - quota exhausted, revoked,
    /// or unpaid - as opposed to a fault that would repeat on every key.</summary>
    private static bool IsKeyRejection(HttpStatusCode status) =>
        status is HttpStatusCode.TooManyRequests
            or HttpStatusCode.Unauthorized
            or HttpStatusCode.Forbidden
            or HttpStatusCode.PaymentRequired;

    /// <summary>
    /// Nulls are omitted from the payload (see <see cref="JsonIgnoreCondition.WhenWritingNull"/>),
    /// so a call that asks for nothing extra sends byte-for-byte the request this service sent
    /// before these fields existed.
    /// </summary>
    private record GroqChatRequest(
        string Model,
        List<GroqChatMessage> Messages,
        [property: JsonPropertyName("max_tokens")] int MaxTokens,
        [property: JsonPropertyName("response_format")]
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        GroqResponseFormat? ResponseFormat,
        [property: JsonPropertyName("temperature")]
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        double? Temperature,
        [property: JsonPropertyName("seed")]
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        int? Seed);

    private record GroqResponseFormat([property: JsonPropertyName("type")] string Type);

    private record GroqChatMessage(string Role, string Content);

    private record GroqChatResponse(List<GroqChatChoice>? Choices, GroqUsage? Usage);

    private record GroqChatChoice(GroqChatMessage? Message);

    private record GroqUsage([property: JsonPropertyName("total_tokens")] int TotalTokens);

    private record GroqErrorResponse(GroqErrorDetail? Error);

    private record GroqErrorDetail(string? Code, string? Message);
}

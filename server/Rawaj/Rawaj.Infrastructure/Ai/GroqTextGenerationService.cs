using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Policies;

namespace Rawaj.Infrastructure.Ai;

public class GroqTextGenerationService(
    IHttpClientFactory httpClientFactory,
    IOptions<GroqSettings> settings,
    ILogger<GroqTextGenerationService> logger) : IAiTextGenerationService
{
    private readonly GroqSettings _settings = settings.Value;

    // static, not instance: this service is registered Scoped (DependencyInjection.cs), so a new
    // instance exists per isolated pipeline-stage scope — an instance field would reset to the same
    // starting key on every single call and never actually rotate. Interlocked.Increment is what
    // makes this safe across the genuinely concurrent calls several stages dispatched together make.
    private static int _nextKeyIndex = -1;

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

        // Rejected here rather than by Groq, when we can already tell it cannot fit. Sending it
        // anyway costs a round-trip per configured key — IsKeyRejection treats the resulting 429 as
        // a key problem and rotates — and returns an error phrased as a rate limit, which is exactly
        // the wrong mental model for a request that will never fit no matter how long you wait.
        if (AiTokenEstimator.ExceedsBudget(prompt, _settings.MaxTokens, _settings.TokensPerMinuteLimit, out var estimated))
        {
            logger.LogWarning(
                "Groq request skipped before sending: about {Estimated} tokens (prompt plus a {MaxTokens}-token completion) " +
                "against a configured limit of {Limit}.",
                estimated, _settings.MaxTokens, _settings.TokensPerMinuteLimit);

            // Phrased to match Groq's own wording on purpose: AiPipelinePolicy.ClassifyProviderError
            // keys off "request too large"/"reduce your message size" to mark this non-retryable, so
            // a locally-detected oversize and a provider-detected one take the same path.
            return AiTextGenerationResult.Failure(
                $"Request too large for {model}: about {estimated} tokens against a limit of " +
                $"{_settings.TokensPerMinuteLimit}. Reduce your message size.");
        }

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

        // Round-robin the starting key rather than always beginning at apiKeys[0]: each key belongs
        // to a separate Groq organization with its own independent TPM budget, so spreading normal
        // (non-failure) traffic across all of them lets concurrent stage calls avoid piling onto the
        // one budget key #1 would otherwise take alone. A failure still falls back through the rest
        // of the list in order, wrapping around — this only changes which key is tried *first*.
        var startIndex = (int)((uint)Interlocked.Increment(ref _nextKeyIndex) % apiKeys.Count);

        foreach (var offset in Enumerable.Range(0, apiKeys.Count))
        {
            var apiKey = apiKeys[(startIndex + offset) % apiKeys.Count];
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

                    // Worth retrying on another key: Groq's TPM limit is enforced per-organization
                    // (its own error message names the org), so a key from a different Groq
                    // organization than the one that just got rate-limited can genuinely still
                    // succeed — this deployment's configured keys are a deliberate mix of several
                    // orgs specifically so this rotation has real headroom to fall back on, not just
                    // more requests to the same exhausted budget.
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

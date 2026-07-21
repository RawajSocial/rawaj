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
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            return AiTextGenerationResult.Failure("No Groq API key is configured.");
        }

        var client = httpClientFactory.CreateClient("Groq");
        client.BaseAddress = new Uri(_settings.BaseUrl.TrimEnd('/') + "/");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);

        var request = new GroqChatRequest(_settings.Model, [new GroqChatMessage("user", prompt)], _settings.MaxTokens);

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
            logger.LogWarning(ex, "Groq request threw an exception.");
            return AiTextGenerationResult.Failure("Text generation failed. Please try again.");
        }
    }

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

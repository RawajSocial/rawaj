using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;

namespace Rawaj.Infrastructure.Ai;

public class HuggingFaceImageGenerationService(
    IHttpClientFactory httpClientFactory,
    IOptions<HuggingFaceSettings> settings,
    ILogger<HuggingFaceImageGenerationService> logger) : IAiImageGenerationService
{
    private readonly HuggingFaceSettings _settings = settings.Value;

    public async Task<AiImageGenerationResult> GenerateImageAsync(string prompt, CancellationToken cancellationToken)
    {
        if (_settings.ApiKeys.Count == 0)
        {
            return AiImageGenerationResult.Failure("No HuggingFace API key is configured.");
        }

        string? lastError = null;

        foreach (var apiKey in _settings.ApiKeys)
        {
            var client = httpClientFactory.CreateClient("HuggingFace");
            client.BaseAddress = new Uri(_settings.BaseUrl);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            try
            {
                using var response = await client.PostAsJsonAsync(
                    _settings.ImageModel, new { inputs = prompt }, cancellationToken);

                var contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;

                if (response.IsSuccessStatusCode && contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                {
                    var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                    return AiImageGenerationResult.Success(bytes, contentType);
                }

                lastError = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning("HuggingFace key ending in {KeySuffix} failed with {StatusCode}: {Error}",
                    apiKey[^Math.Min(4, apiKey.Length)..], response.StatusCode, lastError);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                lastError = ex.Message;
                logger.LogWarning(ex, "HuggingFace request threw an exception, trying next key if available.");
            }
        }

        return AiImageGenerationResult.Failure(lastError ?? "Image generation failed.");
    }
}

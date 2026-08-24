using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
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
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            try
            {
                using var response = await client.PostAsJsonAsync(
                    _settings.BaseUrl, new HfImageRequest(_settings.ImageModel, prompt), cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    lastError = await response.Content.ReadAsStringAsync(cancellationToken);
                    logger.LogWarning("HuggingFace key ending in {KeySuffix} failed with {StatusCode}: {Error}",
                        apiKey[^Math.Min(4, apiKey.Length)..], response.StatusCode, lastError);
                    continue;
                }

                var payload = await response.Content.ReadFromJsonAsync<HfImageResponse>(cancellationToken: cancellationToken);
                var b64 = payload?.Data?.FirstOrDefault()?.B64Json;

                if (string.IsNullOrEmpty(b64))
                {
                    lastError = "HuggingFace returned no image data.";
                    continue;
                }

                return AiImageGenerationResult.Success(Convert.FromBase64String(b64), "image/png");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                lastError = ex.Message;
                logger.LogWarning(ex, "HuggingFace request threw an exception, trying next key if available.");
            }
        }

        return AiImageGenerationResult.Failure(lastError ?? "Image generation failed.");
    }

    private record HfImageRequest(string Model, string Prompt);

    private record HfImageResponse(List<HfImageData>? Data);

    private record HfImageData([property: JsonPropertyName("b64_json")] string? B64Json);
}

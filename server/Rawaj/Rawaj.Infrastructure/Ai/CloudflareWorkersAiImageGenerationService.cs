using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;

namespace Rawaj.Infrastructure.Ai;

public class CloudflareWorkersAiImageGenerationService(
    IHttpClientFactory httpClientFactory,
    IOptions<CloudflareWorkersAiSettings> settings,
    ILogger<CloudflareWorkersAiImageGenerationService> logger) : IAiImageGenerationService
{
    private readonly CloudflareWorkersAiSettings _settings = settings.Value;

    public async Task<AiImageGenerationResult> GenerateImageAsync(string prompt, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.AccountId) || string.IsNullOrWhiteSpace(_settings.ApiToken))
        {
            return AiImageGenerationResult.Failure("No Cloudflare Workers AI account id/API token is configured.");
        }

        var client = httpClientFactory.CreateClient("Cloudflare");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiToken);
        var requestUrl = $"{_settings.BaseUrl}/{_settings.AccountId}/ai/run/{_settings.ImageModel}";

        try
        {
            // This model's input schema requires a multipart/form-data body - a plain JSON body is
            // rejected outright with "required properties at '/' are 'multipart'" before the prompt
            // is ever looked at.
            using var content = new MultipartFormDataContent
            {
                { new StringContent(prompt), "prompt" }
            };
            using var response = await client.PostAsync(requestUrl, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning("Cloudflare Workers AI request failed with {StatusCode}: {Error}", response.StatusCode, error);
                return AiImageGenerationResult.Failure(error);
            }

            var payload = await response.Content.ReadFromJsonAsync<CfImageResponse>(cancellationToken: cancellationToken);

            if (payload is not { Success: true } || string.IsNullOrEmpty(payload.Result?.Image))
            {
                var error = payload?.Errors is { Count: > 0 }
                    ? string.Join("; ", payload.Errors.Select(e => e.Message))
                    : "Cloudflare Workers AI returned no image data.";
                logger.LogWarning("Cloudflare Workers AI returned no image data: {Error}", error);
                return AiImageGenerationResult.Failure(error);
            }

            return AiImageGenerationResult.Success(Convert.FromBase64String(payload.Result.Image), "image/jpeg");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Cloudflare Workers AI request threw an exception.");
            return AiImageGenerationResult.Failure(ex.Message);
        }
    }

    private record CfImageResponse(bool Success, CfImageResult? Result, List<CfImageError>? Errors);

    private record CfImageResult(string? Image);

    private record CfImageError([property: JsonPropertyName("message")] string Message);
}

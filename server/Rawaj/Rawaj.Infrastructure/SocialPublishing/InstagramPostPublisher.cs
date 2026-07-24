using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;
using Rawaj.Infrastructure.SocialOAuth;

namespace Rawaj.Infrastructure.SocialPublishing;

/// <summary>
/// Publishes to an Instagram Business account via the Content Publishing API, a two-step
/// create-then-publish flow. Unlike Facebook's /photos endpoint, this API only accepts a public
/// image_url (no binary upload), so the image is first re-hosted via IPublicImageHostingService.
/// Instagram's API also has no "publish later" mechanism, hence SupportsNativeScheduling is
/// false - scheduled posts fall back to the local poller instead.
/// </summary>
public class InstagramPostPublisher(
    IHttpClientFactory httpClientFactory,
    IOptions<MetaOAuthSettings> settings,
    IPublicImageHostingService imageHostingService,
    ILogger<InstagramPostPublisher> logger) : ISocialPublisher
{
    private readonly MetaOAuthSettings _settings = settings.Value;

    public SocialPlatform Platform => SocialPlatform.Instagram;

    public bool SupportsNativeScheduling => false;

    public async Task<PublishResult> PublishAsync(SocialPublishRequest request, CancellationToken cancellationToken)
    {
        if (request.ImageBytes is null)
        {
            return PublishResult.Failure("Instagram requires an image; text-only posts are not supported.");
        }

        if (!imageHostingService.IsConfigured)
        {
            return PublishResult.Failure(
                "Instagram publishing requires PublicImageHosting:PublicBaseUrl to be configured with a publicly reachable URL.");
        }

        var client = httpClientFactory.CreateClient("Meta");

        try
        {
            var imageUrl = await imageHostingService.HostImageAsync(
                request.ImageBytes, request.ImageContentType ?? "image/jpeg", cancellationToken);

            var creationId = await CreateMediaContainerAsync(client, request, imageUrl, cancellationToken);
            if (!creationId.Succeeded)
            {
                return PublishResult.Failure(creationId.ErrorMessage!);
            }

            return await PublishMediaContainerAsync(client, request, creationId.Value!, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Instagram publish threw an exception.");
            return PublishResult.Failure(ex.Message);
        }
    }

    /// <summary>Instagram has no native scheduling, so a post is never handed off ahead of
    /// time — there's nothing on Instagram's side to revoke.</summary>
    public Task<PublishResult> CancelAsync(string accessToken, string externalPostId, CancellationToken cancellationToken) =>
        Task.FromResult(PublishResult.Success(externalPostId));

    private async Task<(bool Succeeded, string? Value, string? ErrorMessage)> CreateMediaContainerAsync(
        HttpClient client, SocialPublishRequest request, string imageUrl, CancellationToken cancellationToken)
    {
        var fields = new Dictionary<string, string>
        {
            ["image_url"] = imageUrl,
            ["caption"] = request.Message,
            ["access_token"] = request.AccessToken
        };

        using var response = await client.PostAsync(
            $"https://graph.facebook.com/{_settings.ApiVersion}/{request.AccountIdExternal}/media",
            new FormUrlEncodedContent(fields),
            cancellationToken);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Instagram media creation failed with {StatusCode}: {Body}", response.StatusCode, body);
            return (false, null, $"Instagram media creation failed with status {(int)response.StatusCode}: {ExtractErrorMessage(body)}");
        }

        var payload = JsonSerializer.Deserialize<InstagramContainerResponse>(body);
        return string.IsNullOrWhiteSpace(payload?.Id)
            ? (false, null, "Instagram returned an empty media creation response.")
            : (true, payload.Id, null);
    }

    private async Task<PublishResult> PublishMediaContainerAsync(
        HttpClient client, SocialPublishRequest request, string creationId, CancellationToken cancellationToken)
    {
        var fields = new Dictionary<string, string>
        {
            ["creation_id"] = creationId,
            ["access_token"] = request.AccessToken
        };

        using var response = await client.PostAsync(
            $"https://graph.facebook.com/{_settings.ApiVersion}/{request.AccountIdExternal}/media_publish",
            new FormUrlEncodedContent(fields),
            cancellationToken);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Instagram media publish failed with {StatusCode}: {Body}", response.StatusCode, body);
            return PublishResult.Failure($"Instagram publish failed with status {(int)response.StatusCode}: {ExtractErrorMessage(body)}");
        }

        var payload = JsonSerializer.Deserialize<InstagramContainerResponse>(body);
        return string.IsNullOrWhiteSpace(payload?.Id)
            ? PublishResult.Failure("Instagram returned an empty publish response.")
            : PublishResult.Success(payload.Id);
    }

    private static string ExtractErrorMessage(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var error) && error.TryGetProperty("message", out var message))
            {
                return message.GetString() ?? body;
            }
        }
        catch (JsonException)
        {
            // fall through and return the raw body
        }

        return body;
    }

    private record InstagramContainerResponse([property: JsonPropertyName("id")] string? Id);
}

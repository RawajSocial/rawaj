using System.Net.Http.Headers;
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
/// Publishes to a Facebook Page's feed using the Page access token obtained during OAuth
/// connection. Text-only posts go through /{page-id}/feed directly. A post with an attached image
/// is a two-step call — see <see cref="PublishPhotoPostAsync"/> for why a single call to
/// /{page-id}/photos isn't enough to get a real Post out of it.
/// </summary>
public class MetaPostPublisher(
    IHttpClientFactory httpClientFactory,
    IOptions<MetaOAuthSettings> settings,
    ILogger<MetaPostPublisher> logger) : ISocialPublisher
{
    private readonly MetaOAuthSettings _settings = settings.Value;

    public SocialPlatform Platform => SocialPlatform.Facebook;

    public bool SupportsNativeScheduling => true;

    public async Task<PublishResult> PublishAsync(SocialPublishRequest request, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("Meta");

        try
        {
            if (request.ImageBytes is not null || request.ImageUrl is not null)
            {
                return await PublishPhotoPostAsync(client, request, cancellationToken);
            }

            using var response = await PublishTextAsync(client, request, cancellationToken);
            return await ParsePublishResponseAsync(response, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Meta publish threw an exception.");
            return PublishResult.Failure(ex.Message);
        }
    }

    /// <summary>
    /// A single call to /{page-id}/photos only adds a photo to the Page's photo library — it does
    /// not reliably create a visible Post entry in the Page's own Posts/Timeline tab for anyone who
    /// isn't a page admin (confirmed live: the photo itself was reachable by direct URL, but the
    /// Page's Posts tab stayed empty for a non-admin viewer). Meta's documented, reliable way to get
    /// an actual Post out of one photo is the same two-step flow used for multi-photo posts: upload
    /// the photo <c>unpublished</c> (nothing visible yet), then create the real post on
    /// /{page-id}/feed referencing it via <c>attached_media</c> — that second call is what produces
    /// a genuine Post object. Scheduling belongs on that second call: the photo itself is never
    /// "scheduled", it just needs to exist (invisible, since <c>published=false</c>) until the post
    /// referencing it goes live.
    /// </summary>
    private async Task<PublishResult> PublishPhotoPostAsync(HttpClient client, SocialPublishRequest request, CancellationToken cancellationToken)
    {
        using var photoResponse = await UploadUnpublishedPhotoAsync(client, request, cancellationToken);
        var photoResult = await ParsePublishResponseAsync(photoResponse, cancellationToken);
        if (!photoResult.Succeeded)
        {
            return PublishResult.Failure($"Photo upload failed: {photoResult.ErrorMessage}");
        }

        var feedFields = new Dictionary<string, string>
        {
            ["message"] = request.Message,
            ["attached_media[0]"] = $"{{\"media_fbid\":\"{photoResult.ExternalPostId}\"}}",
            ["access_token"] = request.AccessToken
        };
        AddSchedulingFields(feedFields, request.ScheduledAt);

        using var feedResponse = await client.PostAsync(
            $"https://graph.facebook.com/{_settings.ApiVersion}/{request.AccountIdExternal}/feed",
            new FormUrlEncodedContent(feedFields), cancellationToken);

        return await ParsePublishResponseAsync(feedResponse, cancellationToken);
    }

    private async Task<PublishResult> ParsePublishResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Meta publish failed with {StatusCode}: {Body}", response.StatusCode, body);
            return PublishResult.Failure($"Meta publish failed with status {(int)response.StatusCode}: {ExtractErrorMessage(body)}");
        }

        var payload = JsonSerializer.Deserialize<MetaPublishResponse>(body);
        var postId = payload?.PostId ?? payload?.Id;

        if (string.IsNullOrWhiteSpace(postId))
        {
            logger.LogWarning("Meta publish returned no post id. Raw body: {Body}", body);
            return PublishResult.Failure("Meta returned an empty publish response.");
        }

        logger.LogInformation("Meta publish succeeded. Raw body: {Body}", body);
        return PublishResult.Success(postId);
    }

    public async Task<PublishResult> CancelAsync(string accessToken, string externalPostId, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("Meta");

        try
        {
            using var response = await client.DeleteAsync(
                $"https://graph.facebook.com/{_settings.ApiVersion}/{externalPostId}?access_token={Uri.EscapeDataString(accessToken)}",
                cancellationToken);

            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Meta cancel failed with {StatusCode}: {Body}", response.StatusCode, body);
                return PublishResult.Failure($"Meta cancel failed with status {(int)response.StatusCode}: {ExtractErrorMessage(body)}");
            }

            return PublishResult.Success(externalPostId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Meta cancel threw an exception.");
            return PublishResult.Failure(ex.Message);
        }
    }

    private Task<HttpResponseMessage> PublishTextAsync(HttpClient client, SocialPublishRequest request, CancellationToken cancellationToken)
    {
        var fields = new Dictionary<string, string>
        {
            ["message"] = request.Message,
            ["access_token"] = request.AccessToken
        };
        AddSchedulingFields(fields, request.ScheduledAt);

        var form = new FormUrlEncodedContent(fields);

        return client.PostAsync($"https://graph.facebook.com/{_settings.ApiVersion}/{request.AccountIdExternal}/feed", form, cancellationToken);
    }

    /// <summary>Step one of <see cref="PublishPhotoPostAsync"/> — adds the photo to the page's
    /// library as <c>published=false</c>, so it stays invisible until the /feed call that
    /// references it actually publishes. No caption here: the caption becomes the post's own
    /// <c>message</c> on that second call, not this photo's description.</summary>
    private Task<HttpResponseMessage> UploadUnpublishedPhotoAsync(HttpClient client, SocialPublishRequest request, CancellationToken cancellationToken)
    {
        var url = $"https://graph.facebook.com/{_settings.ApiVersion}/{request.AccountIdExternal}/photos";

        if (request.ImageUrl is not null)
        {
            // Cloudinary-hosted (or otherwise already-public) image — let Facebook fetch it
            // itself instead of round-tripping the bytes through us.
            var fields = new Dictionary<string, string>
            {
                ["url"] = request.ImageUrl,
                ["published"] = "false",
                ["access_token"] = request.AccessToken
            };

            return client.PostAsync(url, new FormUrlEncodedContent(fields), cancellationToken);
        }

        var content = new MultipartFormDataContent();

        var imageContent = new ByteArrayContent(request.ImageBytes!);
        imageContent.Headers.ContentType = new MediaTypeHeaderValue(request.ImageContentType ?? "image/jpeg");
        content.Add(imageContent, "source", "image.jpg");
        content.Add(new StringContent("false"), "published");
        content.Add(new StringContent(request.AccessToken), "access_token");

        return client.PostAsync(url, content, cancellationToken);
    }

    private static void AddSchedulingFields(Dictionary<string, string> fields, DateTime? scheduledAt)
    {
        if (!scheduledAt.HasValue)
        {
            return;
        }

        var unixTime = new DateTimeOffset(DateTime.SpecifyKind(scheduledAt.Value, DateTimeKind.Utc)).ToUnixTimeSeconds();
        fields["published"] = "false";
        fields["scheduled_publish_time"] = unixTime.ToString();
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

    private record MetaPublishResponse(
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("post_id")] string? PostId);
}

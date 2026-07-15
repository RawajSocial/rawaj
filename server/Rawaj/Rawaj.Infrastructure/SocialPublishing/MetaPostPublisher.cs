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
/// connection. Posts with an attached image go through /{page-id}/photos (binary upload, so no
/// public image URL is required); text-only posts go through /{page-id}/feed.
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
            using var response = request.ImageBytes is not null
                ? await PublishPhotoAsync(client, request, cancellationToken)
                : await PublishTextAsync(client, request, cancellationToken);

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

            return PublishResult.Success(postId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Meta publish threw an exception.");
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

    private Task<HttpResponseMessage> PublishPhotoAsync(HttpClient client, SocialPublishRequest request, CancellationToken cancellationToken)
    {
        var content = new MultipartFormDataContent();

        var imageContent = new ByteArrayContent(request.ImageBytes!);
        imageContent.Headers.ContentType = new MediaTypeHeaderValue(request.ImageContentType ?? "image/jpeg");
        content.Add(imageContent, "source", "image.jpg");
        content.Add(new StringContent(request.Message), "caption");
        content.Add(new StringContent(request.AccessToken), "access_token");

        if (request.ScheduledAt.HasValue)
        {
            var unixTime = new DateTimeOffset(DateTime.SpecifyKind(request.ScheduledAt.Value, DateTimeKind.Utc)).ToUnixTimeSeconds();
            content.Add(new StringContent("false"), "published");
            content.Add(new StringContent(unixTime.ToString()), "scheduled_publish_time");
        }

        return client.PostAsync($"https://graph.facebook.com/{_settings.ApiVersion}/{request.AccountIdExternal}/photos", content, cancellationToken);
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

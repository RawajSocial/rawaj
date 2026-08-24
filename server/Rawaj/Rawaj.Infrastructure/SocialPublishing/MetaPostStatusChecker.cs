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
/// Facebook photo/attachment-backed scheduled posts (the kind SchedulePost creates via /feed with
/// attached_media — see MetaPostPublisher.PublishPhotoPostAsync) don't expose an is_published field
/// when queried directly by id - confirmed live against the
/// real Graph API, which returns "(#100) Tried accessing nonexisting field (is_published)" for
/// these objects even though the id itself resolves fine. The only reliable signal available with
/// our current permission set is the Page's /scheduled_posts edge: while the post is still there,
/// it hasn't fired yet; once it drops out, a direct id fetch distinguishes "published" (still
/// resolves) from "rejected/deleted" (404s).
/// </summary>
public class MetaPostStatusChecker(
    IHttpClientFactory httpClientFactory,
    IOptions<MetaOAuthSettings> settings,
    ILogger<MetaPostStatusChecker> logger) : ISocialPostStatusChecker
{
    private readonly MetaOAuthSettings _settings = settings.Value;

    public SocialPlatform Platform => SocialPlatform.Facebook;

    public async Task<SocialPostStatusResult> CheckStatusAsync(
        string accountIdExternal, string postId, string accessToken, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("Meta");

        try
        {
            var stillScheduled = await IsInScheduledQueueAsync(client, accountIdExternal, postId, accessToken, cancellationToken);
            if (stillScheduled.Succeeded)
            {
                return SocialPostStatusResult.Success(isPublished: !stillScheduled.Value);
            }

            return SocialPostStatusResult.Failure(stillScheduled.ErrorMessage!);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Meta post status check threw an exception.");
            return SocialPostStatusResult.Failure(ex.Message);
        }
    }

    private async Task<(bool Succeeded, bool Value, string? ErrorMessage)> IsInScheduledQueueAsync(
        HttpClient client, string accountIdExternal, string postId, string accessToken, CancellationToken cancellationToken)
    {
        var url = $"https://graph.facebook.com/{_settings.ApiVersion}/{accountIdExternal}/scheduled_posts" +
                   $"?fields=id&access_token={Uri.EscapeDataString(accessToken)}";

        using var response = await client.GetAsync(url, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Meta scheduled_posts lookup failed with {StatusCode}: {Body}", response.StatusCode, body);
            return (false, false, $"Meta scheduled_posts lookup failed with status {(int)response.StatusCode}: {ExtractErrorMessage(body)}");
        }

        var payload = JsonSerializer.Deserialize<MetaScheduledPostsResponse>(body);
        var stillScheduled = payload?.Data?.Any(p => p.Id == postId || p.Id.EndsWith($"_{postId}", StringComparison.Ordinal)) ?? false;

        if (stillScheduled)
        {
            return (true, true, null);
        }

        // Dropped out of the scheduled queue: confirm whether it published (id still resolves)
        // or was rejected/deleted (404).
        var directUrl = $"https://graph.facebook.com/{_settings.ApiVersion}/{postId}?fields=id&access_token={Uri.EscapeDataString(accessToken)}";
        using var directResponse = await client.GetAsync(directUrl, cancellationToken);

        if (directResponse.IsSuccessStatusCode)
        {
            return (true, false, null);
        }

        var directBody = await directResponse.Content.ReadAsStringAsync(cancellationToken);
        logger.LogWarning("Meta post no longer in scheduled queue and direct lookup failed with {StatusCode}: {Body}", directResponse.StatusCode, directBody);
        return (false, false, $"Post left the scheduled queue and could not be confirmed live: {ExtractErrorMessage(directBody)}");
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

    private record MetaScheduledPostsResponse([property: JsonPropertyName("data")] List<MetaScheduledPostItem>? Data);

    private record MetaScheduledPostItem([property: JsonPropertyName("id")] string Id);
}

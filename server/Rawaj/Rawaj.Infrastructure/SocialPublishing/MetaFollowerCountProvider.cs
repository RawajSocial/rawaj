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
/// Reads the follower count for a connected account. Both a Facebook Page and an Instagram
/// Business Account expose the same field name, <c>followers_count</c>, on their own node - the
/// older Facebook-only <c>fan_count</c> field still works but is the deprecated name for the same
/// number, so <c>followers_count</c> is used for both platforms rather than branching per platform.
/// Registered twice (Facebook, Instagram) the same way <see cref="MetaOAuthProvider"/> is, since the
/// only thing that differs between the two platforms here is which Graph node id gets queried -
/// which the caller already knows via <see cref="Rawaj.Domain.Entities.SocialMedia.SocialAccount.AccountIdExternal"/>.
/// </summary>
public class MetaFollowerCountProvider(
    SocialPlatform platform,
    IHttpClientFactory httpClientFactory,
    IOptions<MetaOAuthSettings> settings,
    ILogger<MetaFollowerCountProvider> logger) : ISocialFollowerCountProvider
{
    private readonly MetaOAuthSettings _settings = settings.Value;

    public SocialPlatform Platform { get; } = platform;

    public async Task<FollowerCountResult> GetFollowerCountAsync(
        string accountIdExternal, string accessToken, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("Meta");

        try
        {
            var url = $"https://graph.facebook.com/{_settings.ApiVersion}/{accountIdExternal}" +
                       "?fields=followers_count" +
                       $"&access_token={Uri.EscapeDataString(accessToken)}";

            using var response = await client.GetAsync(url, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Meta follower count fetch failed with {StatusCode}: {Body}", response.StatusCode, body);
                return FollowerCountResult.Failure($"Follower count fetch failed with status {(int)response.StatusCode}: {ExtractErrorMessage(body)}");
            }

            var payload = JsonSerializer.Deserialize<MetaFollowerCountResponse>(body);
            if (payload?.FollowersCount is null)
            {
                return FollowerCountResult.Failure("Meta returned no followers_count field.");
            }

            return FollowerCountResult.Success(payload.FollowersCount.Value);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Meta follower count fetch threw an exception.");
            return FollowerCountResult.Failure(ex.Message);
        }
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

    private record MetaFollowerCountResponse([property: JsonPropertyName("followers_count")] int? FollowersCount);
}

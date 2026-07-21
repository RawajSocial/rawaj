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
/// Reads engagement counts (likes/comments/shares) for a published Facebook Page post via graph
/// field expansion. This avoids requiring the read_insights permission (impressions/reach need
/// that and Advanced Access review) - it only needs the same page token used to publish.
/// </summary>
public class MetaAnalyticsProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<MetaOAuthSettings> settings,
    ILogger<MetaAnalyticsProvider> logger) : ISocialAnalyticsProvider
{
    private readonly MetaOAuthSettings _settings = settings.Value;

    public SocialPlatform Platform => SocialPlatform.Facebook;

    public async Task<PostMetricsResult> GetMetricsAsync(string postId, string accessToken, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("Meta");

        try
        {
            var url = $"https://graph.facebook.com/{_settings.ApiVersion}/{postId}" +
                       "?fields=likes.summary(true).limit(0),comments.summary(true).limit(0),shares" +
                       $"&access_token={Uri.EscapeDataString(accessToken)}";

            using var response = await client.GetAsync(url, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Meta metrics fetch failed with {StatusCode}: {Body}", response.StatusCode, body);
                return PostMetricsResult.Failure($"Meta metrics fetch failed with status {(int)response.StatusCode}: {ExtractErrorMessage(body)}");
            }

            var payload = JsonSerializer.Deserialize<MetaPostFieldsResponse>(body);

            return PostMetricsResult.Success(
                impressions: null,
                reach: null,
                likes: payload?.Likes?.Summary?.TotalCount,
                comments: payload?.Comments?.Summary?.TotalCount,
                shares: payload?.Shares?.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Meta metrics fetch threw an exception.");
            return PostMetricsResult.Failure(ex.Message);
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

    private record MetaPostFieldsResponse(
        [property: JsonPropertyName("likes")] MetaSummaryField? Likes,
        [property: JsonPropertyName("comments")] MetaSummaryField? Comments,
        [property: JsonPropertyName("shares")] MetaSharesField? Shares);

    private record MetaSummaryField([property: JsonPropertyName("summary")] MetaSummary? Summary);

    private record MetaSummary([property: JsonPropertyName("total_count")] int? TotalCount);

    private record MetaSharesField([property: JsonPropertyName("count")] int? Count);
}

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
/// Reads metrics for a published Facebook Page post from two independent Graph API calls: engagement
/// counts (reactions/comments/shares) via field expansion, which only needs the same page token used to
/// publish, and Views/Unique Viewers via the /insights edge, which needs the read_insights permission
/// (Advanced Access review). The two calls are independent so a failure on one (e.g. read_insights not
/// yet granted for this account) never discards a successful result from the other.
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

        var engagement = await FetchEngagementAsync(client, postId, accessToken, cancellationToken);
        var insights = await FetchInsightsAsync(client, postId, accessToken, cancellationToken);

        var engagementAvailable = engagement.Error is null;
        var insightsAvailable = insights.Error is null;

        if (!engagementAvailable && !insightsAvailable)
        {
            return PostMetricsResult.Failure(
                string.Join(" ", new[] { engagement.Error, insights.Error }.Where(e => e is not null)));
        }

        // A failure on either call must not discard a successful result from the other — combine
        // whichever error(s) occurred purely for diagnostics, without affecting Succeeded below.
        var errorMessage = !engagementAvailable || !insightsAvailable
            ? string.Join(" ", new[] { engagement.Error, insights.Error }.Where(e => e is not null))
            : null;

        return PostMetricsResult.Success(
            views: insights.Views,
            uniqueViewers: insights.UniqueViewers,
            likes: engagement.Likes,
            comments: engagement.Comments,
            shares: engagement.Shares,
            insightsAvailable: insightsAvailable,
            errorMessage: errorMessage);
    }

    private async Task<EngagementFetchResult> FetchEngagementAsync(
        HttpClient client, string postId, string accessToken, CancellationToken cancellationToken)
    {
        try
        {
            // "reactions" (not "likes") is used so every reaction type (Love/Haha/Wow/Sad/Angry/
            // Care) is counted, matching what Facebook's own Post Insights UI reports as
            // "Reactions" - the "likes" edge only counts the literal Like reaction and silently
            // undercounts (often to 0) any post whose reactions are a different type.
            var (status, body) = await FetchFieldsAsync(client, postId, accessToken,
                "reactions.summary(total_count).limit(0),comments.summary(true).limit(0),shares", cancellationToken);

            // The "shares" field is a known Graph API quirk: it's only present in the response at
            // all once a post has at least one share - requesting it explicitly on a post with zero
            // shares throws "(#100) Tried accessing nonexisting field (shares)" and fails the WHOLE
            // combined fields request, taking reactions/comments down with it even though those
            // would have succeeded on their own. Detected and retried without "shares" (defaulted
            // to 0) rather than losing likes/comments over a field that's genuinely just empty.
            if (status is System.Net.HttpStatusCode.BadRequest && IsMissingSharesFieldError(body))
            {
                (status, body) = await FetchFieldsAsync(client, postId, accessToken,
                    "reactions.summary(total_count).limit(0),comments.summary(true).limit(0)", cancellationToken);

                if (status is System.Net.HttpStatusCode.OK)
                {
                    var retryPayload = JsonSerializer.Deserialize<MetaPostFieldsResponse>(body);
                    return new EngagementFetchResult(
                        retryPayload?.Reactions?.Summary?.TotalCount,
                        retryPayload?.Comments?.Summary?.TotalCount,
                        0,
                        null);
                }
            }

            if (status is not System.Net.HttpStatusCode.OK)
            {
                logger.LogWarning("Meta engagement fetch failed with {StatusCode}: {Body}", status, body);
                return new EngagementFetchResult(null, null, null,
                    $"Engagement fetch failed with status {(int)status}: {ExtractErrorMessage(body)}");
            }

            var payload = JsonSerializer.Deserialize<MetaPostFieldsResponse>(body);
            return new EngagementFetchResult(
                payload?.Reactions?.Summary?.TotalCount,
                payload?.Comments?.Summary?.TotalCount,
                payload?.Shares?.Count,
                null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Meta engagement fetch threw an exception.");
            return new EngagementFetchResult(null, null, null, ex.Message);
        }
    }

    private async Task<(System.Net.HttpStatusCode Status, string Body)> FetchFieldsAsync(
        HttpClient client, string postId, string accessToken, string fields, CancellationToken cancellationToken)
    {
        var url = $"https://graph.facebook.com/{_settings.ApiVersion}/{postId}" +
                   $"?fields={fields}&access_token={Uri.EscapeDataString(accessToken)}";

        using var response = await client.GetAsync(url, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return (response.StatusCode, body);
    }

    private static bool IsMissingSharesFieldError(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("error", out var error) || !error.TryGetProperty("message", out var message))
            {
                return false;
            }

            var text = message.GetString();
            return text?.Contains("nonexisting field (shares)", StringComparison.OrdinalIgnoreCase) ?? false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private async Task<InsightsFetchResult> FetchInsightsAsync(
        HttpClient client, string postId, string accessToken, CancellationToken cancellationToken)
    {
        try
        {
            var url = $"https://graph.facebook.com/{_settings.ApiVersion}/{postId}/insights" +
                       "?metric=post_media_view,post_total_media_view_unique" +
                       $"&access_token={Uri.EscapeDataString(accessToken)}";

            using var response = await client.GetAsync(url, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                // Best-effort only: the Graph API error-code reference wasn't confirmed (see Phase 1
                // NEEDS VERIFICATION note), so this only distinguishes "looks like a permission
                // problem" from "anything else" to make production logs more actionable — it does not
                // drive different return behavior.
                var looksLikePermissionError = LooksLikePermissionError(body);
                logger.LogWarning(
                    "Meta insights fetch failed with {StatusCode} ({Classification}): {Body}",
                    response.StatusCode,
                    looksLikePermissionError ? "likely permission/scope issue" : "unclassified",
                    body);
                return new InsightsFetchResult(null, null,
                    $"Insights fetch failed with status {(int)response.StatusCode}: {ExtractErrorMessage(body)}");
            }

            var payload = JsonSerializer.Deserialize<MetaInsightsResponse>(body);
            var metrics = payload?.Data ?? [];

            var views = metrics.FirstOrDefault(m => m.Name == "post_media_view")?.Values?.FirstOrDefault()?.Value;
            var uniqueViewers = metrics.FirstOrDefault(m => m.Name == "post_total_media_view_unique")?.Values?.FirstOrDefault()?.Value;

            return new InsightsFetchResult(views, uniqueViewers, null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Meta insights fetch threw an exception.");
            return new InsightsFetchResult(null, null, ex.Message);
        }
    }

    private static bool LooksLikePermissionError(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("error", out var error))
            {
                return false;
            }

            var type = error.TryGetProperty("type", out var t) ? t.GetString() : null;
            var message = error.TryGetProperty("message", out var m) ? m.GetString() : null;

            return string.Equals(type, "OAuthException", StringComparison.OrdinalIgnoreCase)
                   || (message?.Contains("permission", StringComparison.OrdinalIgnoreCase) ?? false)
                   || (message?.Contains("read_insights", StringComparison.OrdinalIgnoreCase) ?? false);
        }
        catch (JsonException)
        {
            return false;
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

    private sealed record EngagementFetchResult(int? Likes, int? Comments, int? Shares, string? Error);

    private sealed record InsightsFetchResult(long? Views, long? UniqueViewers, string? Error);

    private record MetaPostFieldsResponse(
        [property: JsonPropertyName("reactions")] MetaSummaryField? Reactions,
        [property: JsonPropertyName("comments")] MetaSummaryField? Comments,
        [property: JsonPropertyName("shares")] MetaSharesField? Shares);

    private record MetaSummaryField([property: JsonPropertyName("summary")] MetaSummary? Summary);

    private record MetaSummary([property: JsonPropertyName("total_count")] int? TotalCount);

    private record MetaSharesField([property: JsonPropertyName("count")] int? Count);

    private record MetaInsightsResponse([property: JsonPropertyName("data")] List<MetaInsightMetric>? Data);

    private record MetaInsightMetric(
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("values")] List<MetaInsightValue>? Values);

    private record MetaInsightValue([property: JsonPropertyName("value")] long? Value);
}

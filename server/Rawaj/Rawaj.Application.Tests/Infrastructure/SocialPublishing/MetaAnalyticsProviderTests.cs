using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Rawaj.Infrastructure.SocialOAuth;
using Rawaj.Infrastructure.SocialPublishing;

namespace Rawaj.Application.Tests.Infrastructure.SocialPublishing;

public class MetaAnalyticsProviderTests
{
    private const string EngagementBody =
        """{"reactions":{"summary":{"total_count":10}},"comments":{"summary":{"total_count":3}},"shares":{"count":2}}""";

    private const string InsightsBody =
        """{"data":[{"name":"post_media_view","period":"lifetime","values":[{"value":500}]},{"name":"post_total_media_view_unique","period":"lifetime","values":[{"value":420}]}]}""";

    private const string PermissionErrorBody =
        """{"error":{"message":"(#10) This endpoint requires the read_insights permission.","type":"OAuthException","code":10}}""";

    private static MetaAnalyticsProvider BuildProvider(
        Func<HttpRequestMessage, HttpResponseMessage> engagementResponder,
        Func<HttpRequestMessage, HttpResponseMessage> insightsResponder)
    {
        var handler = new FakeHttpMessageHandler(request =>
            request.RequestUri!.AbsolutePath.Contains("/insights")
                ? insightsResponder(request)
                : engagementResponder(request));

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://graph.facebook.com") };
        var factory = new SingleClientHttpClientFactory(httpClient);
        var settings = Options.Create(new MetaOAuthSettings());

        return new MetaAnalyticsProvider(factory, settings, NullLogger<MetaAnalyticsProvider>.Instance);
    }

    private static HttpResponseMessage Ok(string body) =>
        new(System.Net.HttpStatusCode.OK) { Content = new StringContent(body) };

    private static HttpResponseMessage Error(System.Net.HttpStatusCode status, string body) =>
        new(status) { Content = new StringContent(body) };

    [Fact]
    public async Task GetMetricsAsync_WhenBothCallsSucceed_ReturnsAllFields()
    {
        var provider = BuildProvider(_ => Ok(EngagementBody), _ => Ok(InsightsBody));

        var result = await provider.GetMetricsAsync("post-1", "token", CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.True(result.InsightsAvailable);
        Assert.Equal(500, result.Views);
        Assert.Equal(420, result.UniqueViewers);
        Assert.Equal(10, result.Likes);
        Assert.Equal(3, result.Comments);
        Assert.Equal(2, result.Shares);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task GetMetricsAsync_WhenInsightsPermissionDenied_StillReturnsEngagementFields()
    {
        var provider = BuildProvider(
            _ => Ok(EngagementBody),
            _ => Error(System.Net.HttpStatusCode.BadRequest, PermissionErrorBody));

        var result = await provider.GetMetricsAsync("post-1", "token", CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.False(result.InsightsAvailable);
        Assert.Null(result.Views);
        Assert.Null(result.UniqueViewers);
        Assert.Equal(10, result.Likes);
        Assert.Equal(3, result.Comments);
        Assert.Equal(2, result.Shares);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task GetMetricsAsync_WhenEngagementFailsButInsightsSucceeds_StillReturnsInsightsFields()
    {
        var provider = BuildProvider(
            _ => Error(System.Net.HttpStatusCode.InternalServerError, """{"error":{"message":"transient"}}"""),
            _ => Ok(InsightsBody));

        var result = await provider.GetMetricsAsync("post-1", "token", CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.True(result.InsightsAvailable);
        Assert.Equal(500, result.Views);
        Assert.Equal(420, result.UniqueViewers);
        Assert.Null(result.Likes);
        Assert.Null(result.Comments);
        Assert.Null(result.Shares);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task GetMetricsAsync_WhenBothCallsFail_ReturnsTotalFailure()
    {
        var provider = BuildProvider(
            _ => Error(System.Net.HttpStatusCode.InternalServerError, """{"error":{"message":"down"}}"""),
            _ => Error(System.Net.HttpStatusCode.InternalServerError, """{"error":{"message":"down"}}"""));

        var result = await provider.GetMetricsAsync("post-1", "token", CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.False(result.InsightsAvailable);
        Assert.Null(result.Views);
        Assert.Null(result.Likes);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task GetMetricsAsync_NeverExposesDeprecatedFieldNames()
    {
        // Regression guard for the confirmed terminology decision: PostMetricsResult must never
        // carry Impressions/Reach again. This is a compile-time guarantee (no such properties
        // exist), asserted here by confirming Views/UniqueViewers are the only field names present.
        var resultType = typeof(Rawaj.Application.Common.Models.PostMetricsResult);
        Assert.Null(resultType.GetProperty("Impressions"));
        Assert.Null(resultType.GetProperty("Reach"));
        Assert.NotNull(resultType.GetProperty("Views"));
        Assert.NotNull(resultType.GetProperty("UniqueViewers"));
    }

    private sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(responder(request));
    }

    private sealed class SingleClientHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }
}

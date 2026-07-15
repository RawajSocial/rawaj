using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Infrastructure.SocialOAuth;

public class LinkedInOAuthProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<LinkedInOAuthSettings> settings,
    ILogger<LinkedInOAuthProvider> logger) : ISocialOAuthProvider
{
    private readonly LinkedInOAuthSettings _settings = settings.Value;

    public SocialPlatform Platform => SocialPlatform.Linkedin;

    public string BuildAuthorizationUrl(string state, string redirectUri)
    {
        var query = string.Join('&',
            "response_type=code",
            $"client_id={Uri.EscapeDataString(_settings.ClientId)}",
            $"redirect_uri={Uri.EscapeDataString(redirectUri)}",
            $"state={Uri.EscapeDataString(state)}",
            $"scope={Uri.EscapeDataString(string.Join(' ', _settings.Scopes))}");

        return $"https://www.linkedin.com/oauth/v2/authorization?{query}";
    }

    public async Task<OAuthTokenExchangeResult> ExchangeCodeAsync(string code, string redirectUri, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("LinkedIn");

        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
            ["client_id"] = _settings.ClientId,
            ["client_secret"] = _settings.ClientSecret
        });

        try
        {
            using var response = await client.PostAsync("https://www.linkedin.com/oauth/v2/accessToken", form, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning("LinkedIn token exchange failed with {StatusCode}: {Body}", response.StatusCode, body);
                return OAuthTokenExchangeResult.Failure($"LinkedIn token exchange failed with status {(int)response.StatusCode}.");
            }

            var payload = await response.Content.ReadFromJsonAsync<LinkedInTokenResponse>(cancellationToken: cancellationToken);
            if (payload?.AccessToken is null)
            {
                return OAuthTokenExchangeResult.Failure("LinkedIn returned an empty token response.");
            }

            var expiresAt = payload.ExpiresIn.HasValue ? DateTime.UtcNow.AddSeconds(payload.ExpiresIn.Value) : (DateTime?)null;

            return OAuthTokenExchangeResult.Success(payload.AccessToken, payload.RefreshToken, expiresAt);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "LinkedIn token exchange threw an exception.");
            return OAuthTokenExchangeResult.Failure(ex.Message);
        }
    }

    public async Task<ConnectedAccountProfile> GetAccountProfileAsync(string accessToken, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("LinkedIn");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        try
        {
            using var response = await client.GetAsync("https://api.linkedin.com/v2/userinfo", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return ConnectedAccountProfile.Failure($"Failed to load the LinkedIn profile (status {(int)response.StatusCode}).");
            }

            var payload = await response.Content.ReadFromJsonAsync<LinkedInUserInfoResponse>(cancellationToken: cancellationToken);
            if (payload?.Sub is null)
            {
                return ConnectedAccountProfile.Failure("LinkedIn returned an empty profile response.");
            }

            return ConnectedAccountProfile.Success(payload.Sub, payload.Name ?? payload.Sub);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "LinkedIn profile lookup threw an exception.");
            return ConnectedAccountProfile.Failure(ex.Message);
        }
    }

    public Task<OAuthTokenExchangeResult> RefreshTokenAsync(string currentAccessToken, CancellationToken cancellationToken) =>
        // Standard LinkedIn apps only get 60-day access tokens with no refresh grant; refresh
        // tokens are only issued to apps with Marketing Developer Platform access, which this app
        // doesn't have. Reconnection via the full OAuth flow is the only option here.
        Task.FromResult(OAuthTokenExchangeResult.Failure(
            "LinkedIn does not support token refresh for this app tier; the account must be reconnected."));

    private record LinkedInTokenResponse(
        [property: JsonPropertyName("access_token")] string? AccessToken,
        [property: JsonPropertyName("expires_in")] int? ExpiresIn,
        [property: JsonPropertyName("refresh_token")] string? RefreshToken);

    private record LinkedInUserInfoResponse(string? Sub, string? Name);
}

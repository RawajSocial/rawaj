using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Infrastructure.SocialOAuth;

/// <summary>
/// Handles OAuth for both Facebook Pages and Instagram Business accounts, since both are
/// authorized through the same Meta app and "Facebook Login for Business" dialog. Instagram
/// accounts are connected to a Facebook Page, so profile resolution fetches the user's first
/// Page and, for Instagram, that Page's linked Instagram Business Account.
/// </summary>
public class MetaOAuthProvider(
    SocialPlatform platform,
    IHttpClientFactory httpClientFactory,
    IOptions<MetaOAuthSettings> settings,
    ILogger<MetaOAuthProvider> logger) : ISocialOAuthProvider
{
    private readonly MetaOAuthSettings _settings = settings.Value;

    public SocialPlatform Platform { get; } = platform;

    public string BuildAuthorizationUrl(string state, string redirectUri)
    {
        var scopes = Platform == SocialPlatform.Instagram ? _settings.InstagramScopes : _settings.FacebookScopes;

        var query = string.Join('&',
            $"client_id={Uri.EscapeDataString(_settings.ClientId)}",
            $"redirect_uri={Uri.EscapeDataString(redirectUri)}",
            $"state={Uri.EscapeDataString(state)}",
            $"scope={Uri.EscapeDataString(string.Join(',', scopes))}",
            "response_type=code");

        return $"https://www.facebook.com/{_settings.ApiVersion}/dialog/oauth?{query}";
    }

    public async Task<OAuthTokenExchangeResult> ExchangeCodeAsync(string code, string redirectUri, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("Meta");

        var query = string.Join('&',
            $"client_id={Uri.EscapeDataString(_settings.ClientId)}",
            $"client_secret={Uri.EscapeDataString(_settings.ClientSecret)}",
            $"redirect_uri={Uri.EscapeDataString(redirectUri)}",
            $"code={Uri.EscapeDataString(code)}");

        try
        {
            using var response = await client.GetAsync(
                $"https://graph.facebook.com/{_settings.ApiVersion}/oauth/access_token?{query}", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning("Meta token exchange failed with {StatusCode}: {Body}", response.StatusCode, body);
                return OAuthTokenExchangeResult.Failure($"Meta token exchange failed with status {(int)response.StatusCode}.");
            }

            var payload = await response.Content.ReadFromJsonAsync<MetaTokenResponse>(cancellationToken: cancellationToken);
            if (payload?.AccessToken is null)
            {
                return OAuthTokenExchangeResult.Failure("Meta returned an empty token response.");
            }

            var longLivedResult = await RefreshTokenAsync(payload.AccessToken, cancellationToken);

            var longLivedToken = longLivedResult.Succeeded ? longLivedResult.AccessToken! : payload.AccessToken;
            var expiresAt = longLivedResult.Succeeded
                ? longLivedResult.ExpiresAt
                : (payload.ExpiresIn.HasValue ? DateTime.UtcNow.AddSeconds(payload.ExpiresIn.Value) : (DateTime?)null);

            // The long-lived USER token is stored via RefreshToken (not just AccessToken, which
            // holds the Page token GetAccountProfileAsync resolves) so the background refresher
            // can later re-derive a fresh Page token from it without re-running full OAuth.
            return OAuthTokenExchangeResult.Success(longLivedToken, refreshToken: longLivedToken, expiresAt);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Meta token exchange threw an exception.");
            return OAuthTokenExchangeResult.Failure(ex.Message);
        }
    }

    /// <summary>
    /// Meta lets any still-valid token (short- or long-lived, user or Page) be exchanged again via
    /// the same fb_exchange_token grant to reset its ~60-day expiry window, so this doubles as both
    /// the initial long-lived exchange and the periodic renewal the background refresher calls.
    /// </summary>
    public async Task<OAuthTokenExchangeResult> RefreshTokenAsync(string currentAccessToken, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("Meta");

        var query = string.Join('&',
            "grant_type=fb_exchange_token",
            $"client_id={Uri.EscapeDataString(_settings.ClientId)}",
            $"client_secret={Uri.EscapeDataString(_settings.ClientSecret)}",
            $"fb_exchange_token={Uri.EscapeDataString(currentAccessToken)}");

        try
        {
            using var response = await client.GetAsync(
                $"https://graph.facebook.com/{_settings.ApiVersion}/oauth/access_token?{query}", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning("Meta token refresh failed with {StatusCode}: {Body}", response.StatusCode, body);
                return OAuthTokenExchangeResult.Failure($"Meta token refresh failed with status {(int)response.StatusCode}.");
            }

            var payload = await response.Content.ReadFromJsonAsync<MetaTokenResponse>(cancellationToken: cancellationToken);
            if (payload?.AccessToken is null)
            {
                return OAuthTokenExchangeResult.Failure("Meta returned an empty token refresh response.");
            }

            var expiresAt = payload.ExpiresIn.HasValue ? DateTime.UtcNow.AddSeconds(payload.ExpiresIn.Value) : (DateTime?)null;

            return OAuthTokenExchangeResult.Success(payload.AccessToken, refreshToken: null, expiresAt);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Meta token refresh threw an exception.");
            return OAuthTokenExchangeResult.Failure(ex.Message);
        }
    }

    public async Task<ConnectedAccountProfile> GetAccountProfileAsync(string accessToken, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("Meta");

        try
        {
            using var pagesResponse = await client.GetAsync(
                $"https://graph.facebook.com/{_settings.ApiVersion}/me/accounts?access_token={Uri.EscapeDataString(accessToken)}",
                cancellationToken);

            if (!pagesResponse.IsSuccessStatusCode)
            {
                return ConnectedAccountProfile.Failure($"Failed to list Facebook Pages (status {(int)pagesResponse.StatusCode}).");
            }

            var pages = await pagesResponse.Content.ReadFromJsonAsync<MetaPagesResponse>(cancellationToken: cancellationToken);
            var page = pages?.Data?.FirstOrDefault();
            if (page is null)
            {
                return ConnectedAccountProfile.Failure(
                    "No Facebook Page is available for this account. At least one Page must be managed to connect.");
            }

            if (Platform == SocialPlatform.Facebook)
            {
                return ConnectedAccountProfile.Success(page.Id, page.Name, page.AccessToken);
            }

            using var igResponse = await client.GetAsync(
                $"https://graph.facebook.com/{_settings.ApiVersion}/{page.Id}?fields=instagram_business_account&access_token={Uri.EscapeDataString(page.AccessToken)}",
                cancellationToken);

            if (!igResponse.IsSuccessStatusCode)
            {
                return ConnectedAccountProfile.Failure($"Failed to look up the linked Instagram account (status {(int)igResponse.StatusCode}).");
            }

            var igLink = await igResponse.Content.ReadFromJsonAsync<MetaInstagramLinkResponse>(cancellationToken: cancellationToken);
            if (igLink?.InstagramBusinessAccount is null)
            {
                return ConnectedAccountProfile.Failure(
                    $"The Facebook Page \"{page.Name}\" has no linked Instagram Business account.");
            }

            using var igProfileResponse = await client.GetAsync(
                $"https://graph.facebook.com/{_settings.ApiVersion}/{igLink.InstagramBusinessAccount.Id}?fields=username&access_token={Uri.EscapeDataString(page.AccessToken)}",
                cancellationToken);

            if (!igProfileResponse.IsSuccessStatusCode)
            {
                return ConnectedAccountProfile.Failure($"Failed to load the Instagram profile (status {(int)igProfileResponse.StatusCode}).");
            }

            var igProfile = await igProfileResponse.Content.ReadFromJsonAsync<MetaInstagramProfileResponse>(cancellationToken: cancellationToken);

            return ConnectedAccountProfile.Success(
                igLink.InstagramBusinessAccount.Id, $"@{igProfile?.Username}", page.AccessToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Meta profile lookup threw an exception.");
            return ConnectedAccountProfile.Failure(ex.Message);
        }
    }

    private record MetaTokenResponse(
        [property: JsonPropertyName("access_token")] string? AccessToken,
        [property: JsonPropertyName("expires_in")] int? ExpiresIn);

    private record MetaPagesResponse(List<MetaPage>? Data);

    private record MetaPage(string Id, string Name, [property: JsonPropertyName("access_token")] string AccessToken);

    private record MetaInstagramLinkResponse(
        [property: JsonPropertyName("instagram_business_account")] MetaInstagramAccountId? InstagramBusinessAccount);

    private record MetaInstagramAccountId(string Id);

    private record MetaInstagramProfileResponse(string Username);
}

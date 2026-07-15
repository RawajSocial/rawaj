using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Common.Interfaces;

public interface ISocialOAuthProvider
{
    SocialPlatform Platform { get; }

    string BuildAuthorizationUrl(string state, string redirectUri);

    Task<OAuthTokenExchangeResult> ExchangeCodeAsync(string code, string redirectUri, CancellationToken cancellationToken);

    Task<ConnectedAccountProfile> GetAccountProfileAsync(string accessToken, CancellationToken cancellationToken);

    /// <summary>
    /// Extends the lifetime of a still-valid token before it expires. Platforms without a
    /// refresh mechanism available to this app tier (e.g. LinkedIn without Marketing Developer
    /// Platform access) return Failure with an explanation rather than silently no-oping.
    /// </summary>
    Task<OAuthTokenExchangeResult> RefreshTokenAsync(string currentAccessToken, CancellationToken cancellationToken);
}

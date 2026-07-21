namespace Rawaj.Application.Common.Models;

public class OAuthTokenExchangeResult
{
    public bool Succeeded { get; }
    public string? AccessToken { get; }
    public string? RefreshToken { get; }
    public DateTime? ExpiresAt { get; }
    public string? ErrorMessage { get; }

    private OAuthTokenExchangeResult(bool succeeded, string? accessToken, string? refreshToken, DateTime? expiresAt, string? errorMessage)
    {
        Succeeded = succeeded;
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        ExpiresAt = expiresAt;
        ErrorMessage = errorMessage;
    }

    public static OAuthTokenExchangeResult Success(string accessToken, string? refreshToken, DateTime? expiresAt) =>
        new(true, accessToken, refreshToken, expiresAt, null);

    public static OAuthTokenExchangeResult Failure(string errorMessage) => new(false, null, null, null, errorMessage);
}

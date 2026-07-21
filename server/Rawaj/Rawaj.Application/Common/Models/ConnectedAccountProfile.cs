namespace Rawaj.Application.Common.Models;

public class ConnectedAccountProfile
{
    public bool Succeeded { get; }
    public string? AccountIdExternal { get; }
    public string? AccountName { get; }

    /// <summary>
    /// Some platforms (Meta) issue a separate access token scoped to the specific page/account
    /// being connected, distinct from the user token used to authenticate. When set, this token
    /// should be stored instead of the original user access token.
    /// </summary>
    public string? AccountAccessToken { get; }

    public string? ErrorMessage { get; }

    private ConnectedAccountProfile(bool succeeded, string? accountIdExternal, string? accountName, string? accountAccessToken, string? errorMessage)
    {
        Succeeded = succeeded;
        AccountIdExternal = accountIdExternal;
        AccountName = accountName;
        AccountAccessToken = accountAccessToken;
        ErrorMessage = errorMessage;
    }

    public static ConnectedAccountProfile Success(string accountIdExternal, string accountName, string? accountAccessToken = null) =>
        new(true, accountIdExternal, accountName, accountAccessToken, null);

    public static ConnectedAccountProfile Failure(string errorMessage) => new(false, null, null, null, errorMessage);
}

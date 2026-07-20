namespace Rawaj.Application.Common.Models;

public enum RefreshTokenOutcome
{
    Success,
    NotFound,
    Expired,
    ReusedRevoked
}

public record RefreshTokenIssueResult(string RawToken, DateTime ExpiresAt);

public record RefreshTokenValidationResult(
    RefreshTokenOutcome Outcome,
    Guid UserId,
    string? NewRawToken,
    DateTime NewExpiresAt);

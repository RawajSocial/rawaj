using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Common.Interfaces;

public interface IRefreshTokenService
{
    Task<RefreshTokenIssueResult> IssueAsync(Guid userId, CancellationToken cancellationToken);

    Task<RefreshTokenValidationResult> ValidateAndRotateAsync(string rawToken, CancellationToken cancellationToken);

    Task RevokeAsync(string rawToken, CancellationToken cancellationToken);

    Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken);
}

using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Entities.Auth;

namespace Rawaj.Application.Common.Policies;

/// <summary>
/// Refresh tokens are stored as a SHA-256 hash, never in plaintext (same reasoning as password
/// storage) - only the raw value handed back to the client at issuance can be matched later.
/// Rotation on every use (old token revoked, new one issued) limits the damage of a leaked token
/// to a single use before the swap is detectable.
/// </summary>
public static class RefreshTokenPolicy
{
    private const int DefaultExpiryDays = 30;

    /// <summary>
    /// Adds a new refresh token to the dbContext (caller SaveChanges) and returns the raw value.
    /// </summary>
    public static string Issue(IApplicationDbContext dbContext, Guid userId, int expiryDays = DefaultExpiryDays)
    {
        var rawToken = GenerateRawToken();
        var now = DateTime.UtcNow;

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = Hash(rawToken),
            ExpiresAt = now.AddDays(expiryDays),
            CreatedAt = now
        });

        return rawToken;
    }

    /// <summary>
    /// Validates a raw refresh token and, if valid, revokes it and issues a replacement in the
    /// same call (caller still SaveChanges once). Returns the owning user id and the new raw
    /// token on success.
    /// </summary>
    public static async Task<Result<(Guid UserId, string NewRawToken)>> RotateAsync(
        IApplicationDbContext dbContext, string rawToken, CancellationToken cancellationToken, int expiryDays = DefaultExpiryDays)
    {
        var tokenHash = Hash(rawToken);

        var existing = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (existing is null || existing.RevokedAt is not null || existing.ExpiresAt <= DateTime.UtcNow)
        {
            return Result<(Guid, string)>.Failure("Invalid or expired refresh token.");
        }

        existing.RevokedAt = DateTime.UtcNow;

        var newRawToken = Issue(dbContext, existing.UserId, expiryDays);

        return Result<(Guid, string)>.Success((existing.UserId, newRawToken));
    }

    /// <summary>
    /// Revokes a token without issuing a replacement (logout).
    /// </summary>
    public static async Task<bool> RevokeAsync(IApplicationDbContext dbContext, string rawToken, CancellationToken cancellationToken)
    {
        var tokenHash = Hash(rawToken);

        var existing = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && t.RevokedAt == null, cancellationToken);

        if (existing is null)
        {
            return false;
        }

        existing.RevokedAt = DateTime.UtcNow;
        return true;
    }

    private static string GenerateRawToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    private static string Hash(string rawToken) => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawToken)));
}

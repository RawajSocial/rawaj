using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Entities.Identity;

namespace Rawaj.Persistence.Identity;

public class RefreshTokenService(AppDbContext dbContext, IConfiguration configuration) : IRefreshTokenService
{
    public async Task<RefreshTokenIssueResult> IssueAsync(Guid userId, CancellationToken cancellationToken)
    {
        var (rawToken, expiresAt, _) = CreateToken(userId);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new RefreshTokenIssueResult(rawToken, expiresAt);
    }

    public async Task<RefreshTokenValidationResult> ValidateAndRotateAsync(string rawToken, CancellationToken cancellationToken)
    {
        var hash = Hash(rawToken);
        var token = await dbContext.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (token is null)
        {
            return new RefreshTokenValidationResult(RefreshTokenOutcome.NotFound, default, null, default);
        }

        if (token.RevokedAt is not null)
        {
            // Presenting an already-revoked token is a signal the token chain may have been stolen.
            await RevokeAllForUserAsync(token.UserId, cancellationToken);
            return new RefreshTokenValidationResult(RefreshTokenOutcome.ReusedRevoked, token.UserId, null, default);
        }

        var now = DateTime.UtcNow;
        if (token.ExpiresAt < now)
        {
            return new RefreshTokenValidationResult(RefreshTokenOutcome.Expired, token.UserId, null, default);
        }

        var (newRawToken, newExpiresAt, newToken) = CreateToken(token.UserId);

        token.RevokedAt = now;
        token.ReplacedByTokenId = newToken.Id;

        await dbContext.SaveChangesAsync(cancellationToken);

        return new RefreshTokenValidationResult(RefreshTokenOutcome.Success, token.UserId, newRawToken, newExpiresAt);
    }

    public async Task RevokeAsync(string rawToken, CancellationToken cancellationToken)
    {
        var hash = Hash(rawToken);
        var token = await dbContext.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (token is null || token.RevokedAt is not null)
        {
            return;
        }

        token.RevokedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var activeTokens = await dbContext.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.RevokedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private (string RawToken, DateTime ExpiresAt, RefreshToken Entity) CreateToken(Guid userId)
    {
        var refreshTokenExpiryDays = configuration.GetValue<int?>("Jwt:RefreshTokenExpiryDays") ?? 30;
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var expiresAt = DateTime.UtcNow.AddDays(refreshTokenExpiryDays);

        var entity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = Hash(rawToken),
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow
        };
        dbContext.RefreshTokens.Add(entity);

        return (rawToken, expiresAt, entity);
    }

    private static string Hash(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}

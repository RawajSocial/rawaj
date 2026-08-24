using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Entities.Auth;

namespace Rawaj.Application.Common.Policies;

/// <summary>
/// Email verification codes are stored as a SHA-256 hash, never in plaintext (same reasoning as
/// RefreshTokenPolicy) - only the raw code emailed to the user can be matched later.
/// </summary>
public static class EmailOtpPolicy
{
    private const int CodeLength = 6;
    private static readonly TimeSpan Expiry = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(60);
    private const int MaxAttempts = 5;

    /// <summary>
    /// Adds a new OTP code to the dbContext (caller SaveChanges) and returns the raw code, unless
    /// the resend cooldown since the last issued code hasn't elapsed yet.
    /// </summary>
    public static async Task<Result<string>> IssueAsync(
        IApplicationDbContext dbContext, Guid userId, string email, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var lastIssued = await dbContext.EmailOtpCodes
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => (DateTime?)o.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (lastIssued is not null && now - lastIssued.Value < ResendCooldown)
        {
            var secondsLeft = (int)Math.Ceiling((ResendCooldown - (now - lastIssued.Value)).TotalSeconds);
            return Result<string>.Failure($"Please wait {secondsLeft} seconds before requesting a new code.");
        }

        var code = GenerateCode();

        dbContext.EmailOtpCodes.Add(new EmailOtpCode
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Email = email,
            CodeHash = Hash(code),
            Attempts = 0,
            ExpiresAt = now.Add(Expiry),
            CreatedAt = now
        });

        return Result<string>.Success(code);
    }

    /// <summary>
    /// Validates the most recently issued, unconsumed code for the user. Wrong guesses count
    /// against a per-code attempt limit so a leaked/guessed 6-digit code can't be brute-forced.
    /// </summary>
    public static async Task<Result<bool>> VerifyAsync(
        IApplicationDbContext dbContext, Guid userId, string code, CancellationToken cancellationToken)
    {
        var latest = await dbContext.EmailOtpCodes
            .Where(o => o.UserId == userId && o.ConsumedAt == null)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (latest is null || latest.ExpiresAt <= DateTime.UtcNow)
        {
            return Result<bool>.Failure("This code has expired. Please request a new one.");
        }

        if (latest.Attempts >= MaxAttempts)
        {
            return Result<bool>.Failure("Too many incorrect attempts. Please request a new code.");
        }

        if (latest.CodeHash != Hash(code))
        {
            latest.Attempts++;
            return Result<bool>.Failure("Invalid code.");
        }

        latest.ConsumedAt = DateTime.UtcNow;
        return Result<bool>.Success(true);
    }

    private static string GenerateCode() =>
        RandomNumberGenerator.GetInt32(0, (int)Math.Pow(10, CodeLength)).ToString(new string('0', CodeLength));

    private static string Hash(string code) => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(code)));
}

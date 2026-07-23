using System.Security.Cryptography;
using System.Text;

namespace Rawaj.Application.Common.Policies;

/// <summary>
/// Tokens for brand-new-email team invitations (see <see cref="Rawaj.Domain.Entities.Tenants.TenantInvitation"/>).
/// Stored as a SHA-256 hash, same reasoning as <see cref="RefreshTokenPolicy"/> — only the raw
/// value emailed to the invitee can ever be matched back to a row.
/// </summary>
public static class InvitationTokenPolicy
{
    public const int ExpiryDays = 7;

    public static string GenerateRawToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
        .Replace('+', '-').Replace('/', '_').TrimEnd('=');

    public static string Hash(string rawToken) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}

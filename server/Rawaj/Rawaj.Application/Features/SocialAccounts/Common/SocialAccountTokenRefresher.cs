using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Policies;
using Rawaj.Domain.Entities.SocialMedia;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.SocialAccounts.Common;

/// <summary>
/// Renews social account tokens before they expire. For Meta, RefreshTokenEnc holds the
/// long-lived USER token (not the Page token actually used to publish, stored in Token) - see
/// MetaOAuthProvider.ExchangeCodeAsync. Refreshing means: extend that user token's ~60-day window,
/// then re-derive a fresh Page token from it via GetAccountProfileAsync, exactly mirroring the
/// original connect flow. If a refresh ever fails, the account owner is notified to reconnect
/// manually instead of silently going stale.
/// </summary>
public static class SocialAccountTokenRefresher
{
    private static readonly TimeSpan RefreshWindow = TimeSpan.FromDays(7);

    public static async Task RefreshDueTokensAsync(
        IApplicationDbContext dbContext,
        ITokenEncryptor tokenEncryptor,
        IEnumerable<ISocialOAuthProvider> providers,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow.Add(RefreshWindow);

        var dueAccounts = await dbContext.SocialAccounts
            .Where(s => s.IsActive && s.TokenExpiresAt != null && s.TokenExpiresAt <= cutoff)
            .ToListAsync(cancellationToken);

        foreach (var account in dueAccounts)
        {
            var provider = providers.FirstOrDefault(p => p.Platform == account.Platform);
            if (provider is null)
            {
                continue;
            }

            var (succeeded, errorMessage) = await TryRefreshAsync(dbContext, tokenEncryptor, provider, account, cancellationToken);

            if (!succeeded)
            {
                logger.LogWarning(
                    "Token refresh failed for social account {SocialAccountId} ({Platform}): {Error}",
                    account.Id, account.Platform, errorMessage);
                await NotifyReconnectionNeededAsync(dbContext, account, cancellationToken);
            }
        }

        if (dueAccounts.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task<(bool Succeeded, string? ErrorMessage)> TryRefreshAsync(
        IApplicationDbContext dbContext,
        ITokenEncryptor tokenEncryptor,
        ISocialOAuthProvider provider,
        SocialAccount account,
        CancellationToken cancellationToken)
    {
        var tokenToRefresh = account.RefreshTokenEnc is not null
            ? tokenEncryptor.Decrypt(account.RefreshTokenEnc)
            : tokenEncryptor.Decrypt(account.Token);

        var refreshed = await provider.RefreshTokenAsync(tokenToRefresh, cancellationToken);
        if (!refreshed.Succeeded)
        {
            return (false, refreshed.ErrorMessage);
        }

        var profile = await provider.GetAccountProfileAsync(refreshed.AccessToken!, cancellationToken);

        account.Token = tokenEncryptor.Encrypt(
            profile.Succeeded && profile.AccountAccessToken is not null ? profile.AccountAccessToken : refreshed.AccessToken!);
        account.RefreshTokenEnc = tokenEncryptor.Encrypt(refreshed.AccessToken!);
        account.TokenExpiresAt = refreshed.ExpiresAt;
        account.LastVerifiedAt = DateTime.UtcNow;

        return (true, null);
    }

    private static async Task NotifyReconnectionNeededAsync(
        IApplicationDbContext dbContext, SocialAccount account, CancellationToken cancellationToken)
    {
        var brand = await dbContext.TenantBrandProfiles
            .FirstOrDefaultAsync(b => b.Id == account.BrandProfileId, cancellationToken);
        if (brand is null)
        {
            return;
        }

        var ownerIds = await dbContext.TenantMembers
            .Where(m => m.TenantId == brand.TenantId && m.Role == TenantMemberRole.Owner)
            .Select(m => m.UserId)
            .ToListAsync(cancellationToken);

        foreach (var ownerId in ownerIds)
        {
            NotificationPublisher.Notify(
                dbContext,
                ownerId,
                brand.Id,
                NotificationType.Warning,
                NotificationCategory.System,
                $"{account.Platform} connection needs attention",
                $"We couldn't refresh the access token for {account.AccountName} on {account.Platform}. Please reconnect the account.",
                account.Id,
                "social_account");
        }
    }
}

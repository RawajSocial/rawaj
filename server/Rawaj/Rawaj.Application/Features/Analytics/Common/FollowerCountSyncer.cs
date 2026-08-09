using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Domain.Entities.SocialMedia;

namespace Rawaj.Application.Features.Analytics.Common;

/// <summary>
/// Refreshes SocialAccount.FollowerCount from the platform. Unlike PostAnalyticsSyncer this
/// updates the account row in place rather than appending a history row - a follower count is a
/// current-state fact about the account, not a per-post event, so there is nothing to keep a
/// snapshot history of yet.
/// </summary>
public static class FollowerCountSyncer
{
    public record SyncOutcome(bool Succeeded, string? ErrorMessage);

    public static Task<List<Guid>> GetDueAccountIdsAsync(
        IApplicationDbContext dbContext, DateTime cutoff, CancellationToken cancellationToken) =>
        dbContext.SocialAccounts
            .Where(a => a.IsActive)
            .Where(a => a.FollowerCountSyncedAt == null || a.FollowerCountSyncedAt < cutoff)
            .Select(a => a.Id)
            .ToListAsync(cancellationToken);

    public static async Task<SyncOutcome> SyncOneAsync(
        IApplicationDbContext dbContext,
        ITokenEncryptor tokenEncryptor,
        IEnumerable<ISocialFollowerCountProvider> followerCountProviders,
        SocialAccount socialAccount,
        CancellationToken cancellationToken)
    {
        var provider = followerCountProviders.FirstOrDefault(p => p.Platform == socialAccount.Platform);
        if (provider is null)
        {
            return new SyncOutcome(false, $"Follower count is not yet supported for {socialAccount.Platform}.");
        }

        var accessToken = tokenEncryptor.Decrypt(socialAccount.Token);
        var result = await provider.GetFollowerCountAsync(socialAccount.AccountIdExternal, accessToken, cancellationToken);

        if (!result.Succeeded)
        {
            return new SyncOutcome(false, result.ErrorMessage);
        }

        socialAccount.FollowerCount = result.FollowerCount;
        socialAccount.FollowerCountSyncedAt = DateTime.UtcNow;

        return new SyncOutcome(true, null);
    }
}

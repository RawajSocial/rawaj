using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Domain.Entities.SocialMedia;

namespace Rawaj.Application.Features.Analytics.Common;

/// <summary>
/// Refreshes SocialAccount.FollowerCount from the platform, updating the account row in place for
/// cheap "current value" reads, and also appends a FollowerCountSnapshot row so period-over-period
/// growth (e.g. the dashboard's month-over-month follower comparison) can be reconstructed later
/// instead of only ever knowing the latest number.
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

        var now = DateTime.UtcNow;
        socialAccount.FollowerCount = result.FollowerCount;
        socialAccount.FollowerCountSyncedAt = now;

        dbContext.FollowerCountSnapshots.Add(new FollowerCountSnapshot
        {
            Id = Guid.NewGuid(),
            SocialAccountId = socialAccount.Id,
            Platform = socialAccount.Platform,
            RecordedAt = now,
            FollowerCount = result.FollowerCount!.Value,
        });

        return new SyncOutcome(true, null);
    }
}

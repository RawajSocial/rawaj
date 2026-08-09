using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Features.Analytics.Common;

namespace Rawaj.Infrastructure.BackgroundJobs;

/// <summary>
/// Automatically syncs Meta insights for published posts, replacing the manual "sync now" button as
/// the only way analytics ever got refreshed. Follows the AI pipeline jobs' shape
/// (<see cref="AiPipelineWorkerHostedService"/>) rather than <see cref="PostStatusSyncHostedService"/>'s:
/// the due-post query runs in one short-lived scope, then each post is synced concurrently in its own
/// scope via <see cref="PostAnalyticsSyncer"/> - the same helper the manual endpoint uses, so there is
/// exactly one sync implementation.
///
/// <para>Two deliberate departures from the AI pipeline pattern, both because Meta's API is
/// rate-limited and the AI pipeline jobs never call an external rate-limited API:</para>
/// <para>- Concurrency is capped via <see cref="MaxConcurrentSyncs"/> (a small <c>SemaphoreSlim</c>),
/// not the AI pipeline's unconstrained <c>Task.WhenAll</c>.</para>
/// <para>- Retry is purely implicit: a post that fails to sync just falls back into the next tick's
/// due-post query (it's still outside <see cref="SyncWindow"/>) and gets tried again. There is no
/// counted retry field and no terminal "failed" state, unlike <c>PostStatusSyncer</c> - a snapshot is
/// pure history, not a state machine, and a post that can't sync today (revoked token, transient
/// outage) may sync fine next week.</para>
///
/// <para>No companion reaper job exists for this worker, unlike <see cref="AiPipelineReaperHostedService"/>:
/// there is no claimed/leased state here for a crashed instance to abandon - a post picked up by two
/// overlapping ticks just produces two harmless snapshot rows (both real data points), so there is
/// nothing to reclaim.</para>
/// </summary>
public class AnalyticsSyncHostedService(
    IServiceScopeFactory scopeFactory, ILogger<AnalyticsSyncHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(15);

    /// <summary>A post already snapshotted within this window is not due again yet - keeps every
    /// tick from re-fetching every published post ever made.</summary>
    private static readonly TimeSpan SyncWindow = TimeSpan.FromHours(6);

    /// <summary>Caps in-flight Meta calls at once, across every post/account this tick - deliberately
    /// small and hardcoded for now (see Phase 3's Risks note); a real per-token rate limiter is future
    /// work, not required here.</summary>
    private const int MaxConcurrentSyncs = 3;

    /// <summary>Follower counts change slowly, so they're refreshed on a much longer cadence than
    /// post analytics (<see cref="SyncWindow"/>) - no point spending a Meta API call every 15
    /// minutes on a number that moves a handful of times a day.</summary>
    private static readonly TimeSpan FollowerCountSyncWindow = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncDuePostsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Analytics sync worker tick failed.");
            }

            try
            {
                await SyncDueFollowerCountsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Follower count sync worker tick failed.");
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // shutting down
            }
        }
    }

    private async Task SyncDuePostsAsync(CancellationToken cancellationToken)
    {
        List<Guid> duePostIds;

        using (var scope = scopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            var cutoff = DateTime.UtcNow - SyncWindow;

            duePostIds = await PostAnalyticsSyncer.GetDuePostIdsAsync(dbContext, cutoff, cancellationToken);
        }

        if (duePostIds.Count == 0)
        {
            return;
        }

        using var concurrencyLimiter = new SemaphoreSlim(MaxConcurrentSyncs);

        await Task.WhenAll(duePostIds.Select(id => SyncOneAsync(id, concurrencyLimiter, cancellationToken)));
    }

    private async Task SyncOneAsync(Guid scheduledPostId, SemaphoreSlim concurrencyLimiter, CancellationToken cancellationToken)
    {
        await concurrencyLimiter.WaitAsync(cancellationToken);

        try
        {
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            var tokenEncryptor = scope.ServiceProvider.GetRequiredService<ITokenEncryptor>();
            var analyticsProviders = scope.ServiceProvider.GetRequiredService<IEnumerable<ISocialAnalyticsProvider>>();

            // Re-checked here even though the candidate query already filtered on status/PostId: the
            // post could have been unpublished, deleted, or already re-synced by an overlapping tick
            // between that read and this scope opening.
            var scheduledPost = await dbContext.ScheduledPosts.FirstOrDefaultAsync(s => s.Id == scheduledPostId, cancellationToken);
            if (scheduledPost is null)
            {
                return;
            }

            var outcome = await PostAnalyticsSyncer.SyncOneAsync(
                dbContext, tokenEncryptor, analyticsProviders, scheduledPost, cancellationToken);

            if (!outcome.Succeeded)
            {
                logger.LogWarning(
                    "Analytics sync failed for scheduled post {ScheduledPostId}: {Error}", scheduledPostId, outcome.ErrorMessage);
                return;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Analytics sync threw an exception for scheduled post {ScheduledPostId}.", scheduledPostId);
        }
        finally
        {
            concurrencyLimiter.Release();
        }
    }

    private async Task SyncDueFollowerCountsAsync(CancellationToken cancellationToken)
    {
        List<Guid> dueAccountIds;

        using (var scope = scopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            var cutoff = DateTime.UtcNow - FollowerCountSyncWindow;

            dueAccountIds = await FollowerCountSyncer.GetDueAccountIdsAsync(dbContext, cutoff, cancellationToken);
        }

        if (dueAccountIds.Count == 0)
        {
            return;
        }

        using var concurrencyLimiter = new SemaphoreSlim(MaxConcurrentSyncs);

        await Task.WhenAll(dueAccountIds.Select(id => SyncOneFollowerCountAsync(id, concurrencyLimiter, cancellationToken)));
    }

    private async Task SyncOneFollowerCountAsync(Guid socialAccountId, SemaphoreSlim concurrencyLimiter, CancellationToken cancellationToken)
    {
        await concurrencyLimiter.WaitAsync(cancellationToken);

        try
        {
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            var tokenEncryptor = scope.ServiceProvider.GetRequiredService<ITokenEncryptor>();
            var followerCountProviders = scope.ServiceProvider.GetRequiredService<IEnumerable<ISocialFollowerCountProvider>>();

            var socialAccount = await dbContext.SocialAccounts.FirstOrDefaultAsync(a => a.Id == socialAccountId, cancellationToken);
            if (socialAccount is null)
            {
                return;
            }

            var outcome = await FollowerCountSyncer.SyncOneAsync(
                dbContext, tokenEncryptor, followerCountProviders, socialAccount, cancellationToken);

            if (!outcome.Succeeded)
            {
                logger.LogWarning(
                    "Follower count sync failed for social account {SocialAccountId}: {Error}", socialAccountId, outcome.ErrorMessage);
                return;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Follower count sync threw an exception for social account {SocialAccountId}.", socialAccountId);
        }
        finally
        {
            concurrencyLimiter.Release();
        }
    }
}

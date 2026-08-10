using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Domain.Entities.SocialMedia;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Analytics.Common;

/// <summary>
/// The single implementation behind both the manual "sync now" endpoint
/// (SyncPostAnalyticsCommandHandler) and the scheduled AnalyticsSyncHostedService - kept here,
/// isolated from both MediatR (the manual path) and the hosted-service shell (the scheduled path),
/// so the two callers can never drift into two different sync behaviors. Mirrors how
/// PostStatusSyncer sits alongside PostStatusSyncHostedService for its own domain.
///
/// <para>Always appends a new PostAnalytics row and never updates or deletes an existing one -
/// snapshots are an append-only history, not current state - and never calls SaveChangesAsync
/// itself, leaving that to the caller (the manual handler saves once per request; the hosted
/// service saves once per post, since each post runs in its own DI scope).</para>
/// </summary>
public static class PostAnalyticsSyncer
{
    public record SyncOutcome(bool Succeeded, string? ErrorMessage, PostAnalytics? Analytics);

    /// <summary>
    /// Published posts with a Meta PostId that haven't been snapshotted since <paramref name="cutoff"/>
    /// - naturally spans every tenant/brand/SocialAccount in one pass, since nothing here scopes to a
    /// single tenant. Used by AnalyticsSyncHostedService's per-tick candidate query.
    /// </summary>
    public static Task<List<Guid>> GetDuePostIdsAsync(
        IApplicationDbContext dbContext, DateTime cutoff, CancellationToken cancellationToken) =>
        dbContext.ScheduledPosts
            .Where(s => s.Status == ScheduledPostStatus.Published && s.PostId != null)
            .Where(s => !s.Analytics.Any(a => a.RecordedAt >= cutoff))
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

    public static async Task<SyncOutcome> SyncOneAsync(
        IApplicationDbContext dbContext,
        ITokenEncryptor tokenEncryptor,
        IEnumerable<ISocialAnalyticsProvider> analyticsProviders,
        ScheduledPost scheduledPost,
        CancellationToken cancellationToken)
    {
        if (scheduledPost.Status != ScheduledPostStatus.Published || string.IsNullOrWhiteSpace(scheduledPost.PostId))
        {
            return new SyncOutcome(false, "Only published posts have analytics to sync.", null);
        }

        var socialAccount = await dbContext.SocialAccounts
            .FirstAsync(s => s.Id == scheduledPost.SocialAccountId, cancellationToken);

        var provider = analyticsProviders.FirstOrDefault(p => p.Platform == socialAccount.Platform);
        if (provider is null)
        {
            return new SyncOutcome(false, $"Analytics are not yet supported for {socialAccount.Platform}.", null);
        }

        var accessToken = tokenEncryptor.Decrypt(socialAccount.Token);
        var metrics = await provider.GetMetricsAsync(scheduledPost.PostId, accessToken, cancellationToken);

        if (!metrics.Succeeded)
        {
            return new SyncOutcome(false, metrics.ErrorMessage ?? "Failed to fetch analytics.", null);
        }

        var engagementRate = metrics.Views is > 0
            ? Math.Round((decimal)((metrics.Likes ?? 0) + (metrics.Comments ?? 0) + (metrics.Shares ?? 0)) / metrics.Views.Value, 4)
            : (decimal?)null;

        var analytics = new PostAnalytics
        {
            Id = Guid.NewGuid(),
            ScheduledPostId = scheduledPost.Id,
            Platform = socialAccount.Platform,
            RecordedAt = DateTime.UtcNow,
            Views = metrics.Views,
            UniqueViewers = metrics.UniqueViewers,
            Likes = metrics.Likes,
            Comments = metrics.Comments,
            Shares = metrics.Shares,
            EngagementRate = engagementRate,
            SyncError = metrics.ErrorMessage
        };

        dbContext.PostAnalytics.Add(analytics);

        return new SyncOutcome(true, null, analytics);
    }
}

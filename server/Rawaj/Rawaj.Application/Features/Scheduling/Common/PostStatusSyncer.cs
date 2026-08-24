using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Policies;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Scheduling.Common;

/// <summary>
/// Confirms posts handed off to a platform's native scheduler (PostId set, Status still Pending -
/// see ScheduledPostPublisher) actually went live. A successful handoff only means the platform
/// accepted the schedule request; the platform can still silently drop it later (Page unpublished,
/// policy violation, manual deletion), which this catches instead of leaving the row Pending
/// forever with no local signal that anything went wrong.
/// </summary>
public static class PostStatusSyncer
{
    /// <summary>
    /// A status check can fail transiently (rate limit, blip). Only treat it as a confirmed
    /// platform-side failure (deleted post, rejected schedule) after this many consecutive failed
    /// checks, so a single flaky call doesn't wrongly mark a post that will still publish fine.
    /// </summary>
    private const int MaxConsecutiveCheckFailures = 5;

    public static async Task SyncNativelyScheduledPostsAsync(
        IApplicationDbContext dbContext,
        ITokenEncryptor tokenEncryptor,
        IEnumerable<ISocialPostStatusChecker> statusCheckers,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var duePostIds = await dbContext.ScheduledPosts
            .Where(s => s.Status == ScheduledPostStatus.Pending && s.PostId != null)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        foreach (var scheduledPostId in duePostIds)
        {
            await SyncOneAsync(dbContext, tokenEncryptor, statusCheckers, scheduledPostId, logger, cancellationToken);
        }

        if (duePostIds.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task SyncOneAsync(
        IApplicationDbContext dbContext,
        ITokenEncryptor tokenEncryptor,
        IEnumerable<ISocialPostStatusChecker> statusCheckers,
        Guid scheduledPostId,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var scheduledPost = await dbContext.ScheduledPosts.FirstOrDefaultAsync(s => s.Id == scheduledPostId, cancellationToken);
        if (scheduledPost is null || scheduledPost.Status != ScheduledPostStatus.Pending || scheduledPost.PostId is null)
        {
            return;
        }

        var socialAccount = await dbContext.SocialAccounts.FirstAsync(s => s.Id == scheduledPost.SocialAccountId, cancellationToken);

        var checker = statusCheckers.FirstOrDefault(c => c.Platform == socialAccount.Platform);
        if (checker is null)
        {
            return;
        }

        var accessToken = tokenEncryptor.Decrypt(socialAccount.Token);
        var status = await checker.CheckStatusAsync(socialAccount.AccountIdExternal, scheduledPost.PostId, accessToken, cancellationToken);

        if (!status.Succeeded)
        {
            logger.LogWarning(
                "Could not confirm publish status for scheduled post {ScheduledPostId} (attempt {Attempt}): {Error}",
                scheduledPostId, scheduledPost.RetryCount + 1, status.ErrorMessage);

            scheduledPost.RetryCount += 1;
            scheduledPost.UpdatedAt = DateTime.UtcNow;

            if (scheduledPost.RetryCount < MaxConsecutiveCheckFailures)
            {
                // Likely transient; leave Status as Pending so the next poll retries.
                return;
            }

            scheduledPost.Status = ScheduledPostStatus.Failed;
            scheduledPost.ErrorMessage = $"Platform confirmation failed after {scheduledPost.RetryCount} attempts: {status.ErrorMessage}";

            var failedContentItem = await dbContext.ContentItems.FirstAsync(c => c.Id == scheduledPost.ContentItemId, cancellationToken);
            NotificationPublisher.Notify(
                dbContext,
                failedContentItem.CreatedBy,
                failedContentItem.BrandProfileId,
                NotificationType.Error,
                NotificationCategory.PostFailed,
                "Post failed to publish",
                $"{socialAccount.Platform} could not confirm your scheduled post published: {status.ErrorMessage}",
                scheduledPost.Id,
                "scheduled_post");

            return;
        }

        // A successful check that finds the post not yet published resets the failure streak.
        scheduledPost.RetryCount = 0;

        if (!status.IsPublished)
        {
            return;
        }

        scheduledPost.Status = ScheduledPostStatus.Published;
        scheduledPost.PublishedAt = DateTime.UtcNow;
        scheduledPost.UpdatedAt = DateTime.UtcNow;

        var contentItem = await dbContext.ContentItems.FirstAsync(c => c.Id == scheduledPost.ContentItemId, cancellationToken);
        contentItem.Status = ContentStatus.Published;

        NotificationPublisher.Notify(
            dbContext,
            contentItem.CreatedBy,
            contentItem.BrandProfileId,
            NotificationType.Success,
            NotificationCategory.PostPublished,
            "Post published",
            $"Your post was published to {socialAccount.Platform} ({socialAccount.AccountName}).",
            scheduledPost.Id,
            "scheduled_post");
    }
}

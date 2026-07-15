using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Policies;
using Rawaj.Domain.Entities.SocialMedia;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Scheduling.Common;

public static class ScheduledPostPublisher
{
    /// <summary>
    /// Publishes a scheduled post immediately, used both by the manual "publish now" endpoint
    /// and as a fallback by the background poller for platforms without native scheduling.
    /// </summary>
    public static Task<Result<ScheduledPost>> PublishNowAsync(
        IApplicationDbContext dbContext,
        ITokenEncryptor tokenEncryptor,
        IEnumerable<ISocialPublisher> publishers,
        Guid scheduledPostId,
        CancellationToken cancellationToken) =>
        ExecuteAsync(dbContext, tokenEncryptor, publishers, scheduledPostId, scheduledAt: null, cancellationToken);

    /// <summary>
    /// Hands the post off to the platform's own native scheduler (e.g. Facebook's
    /// scheduled_publish_time) immediately upon scheduling, so it shows up in the platform's own
    /// scheduled-posts UI and the platform handles the actual publish timing. The local row stays
    /// Pending with the platform's post id recorded — <see cref="ScheduledPost.PostId"/> being set
    /// while Pending is exactly what marks a row as "handed off" rather than "needs local publish."
    /// </summary>
    public static Task<Result<ScheduledPost>> ScheduleNativelyAsync(
        IApplicationDbContext dbContext,
        ITokenEncryptor tokenEncryptor,
        IEnumerable<ISocialPublisher> publishers,
        Guid scheduledPostId,
        DateTime scheduledAt,
        CancellationToken cancellationToken) =>
        ExecuteAsync(dbContext, tokenEncryptor, publishers, scheduledPostId, scheduledAt, cancellationToken);

    private static async Task<Result<ScheduledPost>> ExecuteAsync(
        IApplicationDbContext dbContext,
        ITokenEncryptor tokenEncryptor,
        IEnumerable<ISocialPublisher> publishers,
        Guid scheduledPostId,
        DateTime? scheduledAt,
        CancellationToken cancellationToken)
    {
        var scheduledPost = await dbContext.ScheduledPosts
            .FirstOrDefaultAsync(s => s.Id == scheduledPostId, cancellationToken);
        if (scheduledPost is null)
        {
            return Result<ScheduledPost>.Failure("Scheduled post not found.");
        }

        if (scheduledPost.Status != ScheduledPostStatus.Pending)
        {
            return Result<ScheduledPost>.Failure("Only pending scheduled posts can be published.");
        }

        var contentItem = await dbContext.ContentItems
            .FirstAsync(c => c.Id == scheduledPost.ContentItemId, cancellationToken);
        var socialAccount = await dbContext.SocialAccounts
            .FirstAsync(s => s.Id == scheduledPost.SocialAccountId, cancellationToken);

        var publisher = publishers.FirstOrDefault(p => p.Platform == socialAccount.Platform);
        if (publisher is null)
        {
            return Result<ScheduledPost>.Failure($"Publishing is not yet supported for {socialAccount.Platform}.");
        }

        if (scheduledAt.HasValue && !publisher.SupportsNativeScheduling)
        {
            // This platform has no "publish later" API of its own; leave the row Pending with no
            // PostId so the local background poller recognizes it still needs a local publish
            // once ScheduledAt passes.
            return Result<ScheduledPost>.Success(scheduledPost);
        }

        byte[]? imageBytes = null;
        string? imageContentType = null;

        if (scheduledPost.VisualAssetId is not null)
        {
            var visualAsset = await dbContext.VisualAssets
                .FirstOrDefaultAsync(v => v.Id == scheduledPost.VisualAssetId, cancellationToken);

            if (visualAsset is not null && visualAsset.FileUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                (imageContentType, imageBytes) = ParseDataUrl(visualAsset.FileUrl);
            }
        }

        var accessToken = tokenEncryptor.Decrypt(socialAccount.Token);
        var message = BuildMessage(contentItem.Content, contentItem.Hashtags);

        var publishResult = await publisher.PublishAsync(
            new SocialPublishRequest(accessToken, socialAccount.AccountIdExternal, message, imageBytes, imageContentType, scheduledAt),
            cancellationToken);

        var now = DateTime.UtcNow;

        if (publishResult.Succeeded && scheduledAt is null)
        {
            // Published immediately.
            scheduledPost.Status = ScheduledPostStatus.Published;
            scheduledPost.PostId = publishResult.ExternalPostId;
            scheduledPost.PublishedAt = now;
            scheduledPost.ErrorMessage = null;
            contentItem.Status = ContentStatus.Published;
        }
        else if (publishResult.Succeeded)
        {
            // Handed off to the platform's native scheduler; stays Pending until the platform
            // actually publishes it. PostId being set is what distinguishes this from a row that
            // still needs the local poller to fire it.
            scheduledPost.PostId = publishResult.ExternalPostId;
            scheduledPost.ErrorMessage = null;
        }
        else
        {
            scheduledPost.Status = ScheduledPostStatus.Failed;
            scheduledPost.ErrorMessage = publishResult.ErrorMessage;
            scheduledPost.RetryCount += 1;
        }

        scheduledPost.UpdatedAt = now;

        if (scheduledPost.Status == ScheduledPostStatus.Published)
        {
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
        else if (scheduledPost.Status == ScheduledPostStatus.Failed)
        {
            NotificationPublisher.Notify(
                dbContext,
                contentItem.CreatedBy,
                contentItem.BrandProfileId,
                NotificationType.Error,
                NotificationCategory.PostFailed,
                "Post failed to publish",
                $"Publishing to {socialAccount.Platform} ({socialAccount.AccountName}) failed: {scheduledPost.ErrorMessage}",
                scheduledPost.Id,
                "scheduled_post");
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return publishResult.Succeeded
            ? Result<ScheduledPost>.Success(scheduledPost)
            : Result<ScheduledPost>.Failure(publishResult.ErrorMessage ?? "Publishing failed.");
    }

    private static string BuildMessage(string content, List<string> hashtags)
    {
        if (hashtags.Count == 0)
        {
            return content;
        }

        return content + "\n\n" + string.Join(' ', hashtags.Select(h => $"#{h}"));
    }

    private static (string ContentType, byte[] Bytes) ParseDataUrl(string dataUrl)
    {
        var commaIndex = dataUrl.IndexOf(',');
        var header = dataUrl[..commaIndex];
        var contentType = header[5..header.IndexOf(';')];
        var base64 = dataUrl[(commaIndex + 1)..];

        return (contentType, Convert.FromBase64String(base64));
    }
}

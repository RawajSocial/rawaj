using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Features.Scheduling.Common;

namespace Rawaj.Application.Features.Campaigns.DeleteCampaign;

/// <summary>
/// The external half of a campaign delete: revoke any platform-side scheduled post, then purge the
/// campaign's images from media storage. Lives in Application rather than inside the hosted service
/// so it can be tested without spinning up a BackgroundService — the same split
/// <see cref="ScheduledPostPublisher"/> already uses.
///
/// <para>Idempotent by construction, because the outbox retries the whole message: clearing
/// <c>PostId</c> after a successful revoke makes a re-run skip that post, and Cloudinary treats
/// deleting an already-gone asset as success. Throwing on a failed revoke is deliberate — it hands
/// the message back to the outbox to retry rather than leaving a post live in Facebook's own
/// scheduler, which is the one outcome the user can't see or fix from inside Rawaj.</para>
///
/// <para><c>IgnoreQueryFilters</c> is required throughout: everything here was soft-deleted by the
/// command that queued the message, so ordinary reads no longer see it.</para>
/// </summary>
public static class CampaignCleanupExecutor
{
    public static async Task ExecuteAsync(
        IApplicationDbContext dbContext,
        ITokenEncryptor tokenEncryptor,
        IEnumerable<ISocialPublisher> publishers,
        IMediaStorageService mediaStorage,
        CampaignCleanupPayload payload,
        CancellationToken cancellationToken)
    {
        if (payload.PendingScheduledPostIds.Count > 0)
        {
            var postIds = payload.PendingScheduledPostIds.ToList();
            var posts = await dbContext.ScheduledPosts
                .IgnoreQueryFilters()
                .Where(s => postIds.Contains(s.Id) && s.PostId != null)
                .ToListAsync(cancellationToken);

            foreach (var post in posts)
            {
                var result = await ScheduledPostPublisher.CancelNativeAsync(
                    dbContext, tokenEncryptor, publishers, post, cancellationToken);

                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(result.ErrorMessage ?? "Platform cancel failed.");
                }

                post.PostId = null;
                post.UpdatedAt = DateTime.UtcNow;
            }
        }

        if (payload.MediaPublicIds.Count == 0 || !mediaStorage.IsConfigured)
        {
            return;
        }

        foreach (var publicId in payload.MediaPublicIds)
        {
            await mediaStorage.DeleteAsync(publicId, cancellationToken);
        }
    }
}

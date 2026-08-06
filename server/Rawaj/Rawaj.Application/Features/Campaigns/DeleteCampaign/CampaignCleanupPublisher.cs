using System.Text.Json;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Domain.Entities.Platform;

namespace Rawaj.Application.Features.Campaigns.DeleteCampaign;

/// <summary>
/// Everything the background cleanup needs to finish a campaign delete without re-deriving it from
/// the database. The rows it describes are already soft-deleted by the time this is dispatched, so
/// re-querying them would need <c>IgnoreQueryFilters</c> everywhere and would race with anything
/// else touching them — carrying the ids and public ids in the payload avoids both.
/// </summary>
public record CampaignCleanupPayload(
    Guid CampaignId,
    string CampaignName,
    Guid BrandProfileId,
    Guid RequestedByUserId,
    IReadOnlyList<Guid> PendingScheduledPostIds,
    IReadOnlyList<string> MediaPublicIds);

/// <summary>
/// Queues the slow, external half of a campaign delete — revoking platform-side scheduled posts and
/// purging images from media storage — onto the outbox, so the request that triggered it can return
/// as soon as the database work commits.
///
/// <para>Doing this inline was what made the delete feel broken: a campaign with a dozen images cost
/// a dozen sequential Cloudinary round-trips plus a Graph API call per pending post, all while the
/// user sat on a spinner. None of that work needs the user present, and none of it is what makes the
/// campaign "deleted" — the soft-delete flags are, and those commit in a single SaveChanges.</para>
///
/// <para>Using the outbox rather than fire-and-forget is the point: the message commits in the SAME
/// transaction as the soft delete, so the cleanup can never be lost to a crash between the two, and
/// a failed platform cancel is retried instead of silently leaving a post to publish on Facebook.
/// Both the Cloudinary delete ("not found" counts as success) and <c>CancelNativeAsync</c> (no-op
/// once <c>PostId</c> is cleared) are idempotent, so a retry of a partly-finished message is safe.</para>
/// </summary>
public static class CampaignCleanupPublisher
{
    public const string CampaignCleanupType = "CampaignCleanup";

    public static void Queue(IApplicationDbContext dbContext, CampaignCleanupPayload payload)
    {
        // Nothing external to do — don't make the dispatcher wake up for an empty message.
        if (payload.PendingScheduledPostIds.Count == 0 && payload.MediaPublicIds.Count == 0)
        {
            return;
        }

        dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = CampaignCleanupType,
            PayloadJson = JsonSerializer.Serialize(payload),
            CreatedAt = DateTime.UtcNow,
        });
    }
}

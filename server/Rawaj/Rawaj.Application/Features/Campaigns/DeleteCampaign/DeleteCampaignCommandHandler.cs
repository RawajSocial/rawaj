using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Policies;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Campaigns.DeleteCampaign;

/// <summary>
/// Cascades a campaign delete to everything under it. This is a soft delete, not a SQL row delete —
/// <c>AiPipelineRun</c>/<c>AiArtifact</c>/<c>ContentItem</c>/<c>VisualAsset</c>/<c>ScheduledPost</c>
/// all reference <c>MarketingCampaign</c> with a <c>Restrict</c> foreign key (see
/// <c>MarketingCampaignConfiguration</c> and friends), so a real row delete would either fail outright
/// or require deleting the pipeline's own run history first. Soft-deleting sidesteps all of that: the
/// campaign row stays, `IsDeleted` hides it (and everything cascaded here) from every query via each
/// entity's global query filter, exactly like `DeleteContentItem`/`DeleteVisualAsset` already do for a
/// single item.
///
/// <para><b>This handler only touches the database</b> — one SaveChanges, no network I/O, so the
/// campaign disappears from the user's list immediately instead of holding the request open for a
/// Cloudinary purge and a Graph API call per post. The external work is handed to
/// <see cref="CampaignCleanupPublisher"/>, whose outbox message commits in the same transaction as
/// the soft delete and is picked up by <c>CampaignCleanupHostedService</c> seconds later.</para>
///
/// <para><b>Published posts are left live on their platform</b> — deliberate product decision, not an
/// oversight: deleting a campaign in Rawaj must not silently take down a post that's already public on
/// the tenant's real Facebook/Instagram page. Only the local record is removed.</para>
///
/// <para><b>Pending (not-yet-published) posts are genuinely cancelled on the platform</b>, just not on
/// this thread. Flipping the local status to <c>Cancelled</c> here is what stops Rawaj's own publisher
/// from firing them (the poller only ever picks up Pending rows, and the soft-delete filter hides them
/// besides); the queued cleanup revokes the platform-side handoff for anything Facebook already agreed
/// to publish on its own schedule. <c>PostId</c> is deliberately left intact for the cleanup to use.</para>
/// </summary>
public class DeleteCampaignCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    ICurrentTenantContext currentTenantContext)
    : IRequestHandler<DeleteCampaignCommand, Result<DeleteCampaignResponse>>
{
    public async Task<Result<DeleteCampaignResponse>> Handle(DeleteCampaignCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;
        var userId = currentUserService.UserId!.Value;

        var campaign = await dbContext.MarketingCampaigns
            .FirstOrDefaultAsync(c => c.Id == request.CampaignId && c.BrandProfile.TenantId == tenantId, cancellationToken);
        if (campaign is null)
        {
            return Result<DeleteCampaignResponse>.Failure("Campaign not found.");
        }

        var now = DateTime.UtcNow;

        var scheduledPosts = await dbContext.ScheduledPosts
            .Where(s => s.CampaignId == campaign.Id)
            .ToListAsync(cancellationToken);

        var pendingPostIds = new List<Guid>();
        var publishedLeftLiveCount = 0;

        foreach (var post in scheduledPosts)
        {
            if (post.Status == ScheduledPostStatus.Pending)
            {
                // Only posts actually handed off to the platform need revoking; the rest are stopped
                // outright by the status flip below.
                if (post.PostId is not null)
                {
                    pendingPostIds.Add(post.Id);
                }

                post.Status = ScheduledPostStatus.Cancelled;
            }
            else if (post.Status == ScheduledPostStatus.Published)
            {
                publishedLeftLiveCount++;
            }

            post.IsDeleted = true;
            post.DeletedAt = now;
            post.DeletedBy = userId;
            post.UpdatedAt = now;
        }

        var cancelledCount = scheduledPosts.Count(s => s.Status == ScheduledPostStatus.Cancelled);

        var contentItems = await dbContext.ContentItems
            .Where(c => c.CampaignId == campaign.Id)
            .ToListAsync(cancellationToken);
        var contentItemIds = contentItems.Select(c => c.Id).ToHashSet();

        // Belt-and-braces: catches an asset tagged only via ContentItemId even if CampaignId was
        // somehow never stamped on it — every known creation site sets both, but a query this
        // destructive should not rely on that holding everywhere forever.
        var visualAssets = await dbContext.VisualAssets
            .Where(v => v.CampaignId == campaign.Id || (v.ContentItemId != null && contentItemIds.Contains(v.ContentItemId.Value)))
            .ToListAsync(cancellationToken);

        var mediaPublicIds = new List<string>();

        foreach (var asset in visualAssets)
        {
            if (!string.IsNullOrWhiteSpace(asset.PublicId))
            {
                mediaPublicIds.Add(asset.PublicId);
            }

            asset.IsDeleted = true;
            asset.DeletedAt = now;
            asset.DeletedBy = userId;
        }

        foreach (var item in contentItems)
        {
            item.IsDeleted = true;
            item.DeletedAt = now;
            item.DeletedBy = userId;
            item.UpdatedAt = now;
        }

        campaign.IsDeleted = true;
        campaign.DeletedAt = now;
        campaign.DeletedBy = userId;
        campaign.UpdatedAt = now;

        CampaignCleanupPublisher.Queue(dbContext, new CampaignCleanupPayload(
            campaign.Id, campaign.Name, campaign.BrandProfileId, userId, pendingPostIds, mediaPublicIds));

        NotificationPublisher.Notify(
            dbContext, userId, campaign.BrandProfileId,
            NotificationType.Info, NotificationCategory.System,
            "Campaign deleted",
            $"\"{campaign.Name}\" and its {contentItems.Count} post(s) were permanently deleted.",
            campaign.Id, "marketing_campaign");

        AuditLogger.Log(
            dbContext, tenantId, userId, "campaign.deleted",
            message: $"Deleted campaign \"{campaign.Name}\" — {contentItems.Count} content item(s), " +
                     $"{visualAssets.Count} image(s), {cancelledCount} pending post(s) cancelled, " +
                     $"{publishedLeftLiveCount} published post(s) left live on their platform.",
            entityType: "marketing_campaign", entityId: campaign.Id, brandProfileId: campaign.BrandProfileId);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<DeleteCampaignResponse>.Success(new DeleteCampaignResponse(
            campaign.Id, contentItems.Count, visualAssets.Count, cancelledCount, publishedLeftLiveCount));
    }
}

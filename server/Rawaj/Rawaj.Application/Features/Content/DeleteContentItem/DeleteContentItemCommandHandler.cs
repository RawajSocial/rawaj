using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Content.DeleteContentItem;

public class DeleteContentItemCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    ICurrentTenantContext currentTenantContext,
    IMediaStorageService mediaStorageService)
    : IRequestHandler<DeleteContentItemCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteContentItemCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;

        var contentItem = await dbContext.ContentItems
            .FirstOrDefaultAsync(c => c.Id == request.ContentItemId && c.TenantId == tenantId, cancellationToken);
        if (contentItem is null)
        {
            return Result<bool>.Failure("Content item not found.");
        }

        var hasActiveSchedule = await dbContext.ScheduledPosts.AnyAsync(
            s => s.ContentItemId == contentItem.Id
                && (s.Status == ScheduledPostStatus.Pending || s.Status == ScheduledPostStatus.Published),
            cancellationToken);
        if (hasActiveSchedule)
        {
            return Result<bool>.Failure(
                "This content is scheduled or already published — cancel the scheduled post before deleting it.");
        }

        // A content item's own generated images are removed alongside it — they'd otherwise be
        // left as orphaned rows nobody can reach (still real Cloudinary storage cost, no owner).
        var linkedAssets = await dbContext.VisualAssets
            .Where(v => v.ContentItemId == contentItem.Id)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var userId = currentUserService.UserId;

        foreach (var asset in linkedAssets)
        {
            await DeleteFromStorageIfConfigured(asset, mediaStorageService, cancellationToken);
            asset.IsDeleted = true;
            asset.DeletedAt = now;
            asset.DeletedBy = userId;
        }

        contentItem.IsDeleted = true;
        contentItem.DeletedAt = now;
        contentItem.DeletedBy = userId;
        contentItem.UpdatedAt = now;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }

    private static async Task DeleteFromStorageIfConfigured(
        VisualAsset asset, IMediaStorageService mediaStorageService, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(asset.PublicId) && mediaStorageService.IsConfigured)
        {
            await mediaStorageService.DeleteAsync(asset.PublicId, cancellationToken);
        }
    }
}

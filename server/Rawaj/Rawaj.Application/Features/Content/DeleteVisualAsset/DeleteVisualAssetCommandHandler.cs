using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Content.DeleteVisualAsset;

public class DeleteVisualAssetCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    ICurrentTenantContext currentTenantContext,
    IMediaStorageService mediaStorageService)
    : IRequestHandler<DeleteVisualAssetCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteVisualAssetCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;

        var visualAsset = await dbContext.VisualAssets
            .FirstOrDefaultAsync(a => a.Id == request.VisualAssetId && a.TenantId == tenantId, cancellationToken);
        if (visualAsset is null)
        {
            return Result<bool>.Failure("Visual asset not found.");
        }

        var hasActiveSchedule = await dbContext.ScheduledPosts.AnyAsync(
            s => s.VisualAssetId == visualAsset.Id
                && (s.Status == ScheduledPostStatus.Pending || s.Status == ScheduledPostStatus.Published),
            cancellationToken);
        if (hasActiveSchedule)
        {
            return Result<bool>.Failure(
                "This image is scheduled or already published — cancel the scheduled post before deleting it.");
        }

        if (!string.IsNullOrWhiteSpace(visualAsset.PublicId) && mediaStorageService.IsConfigured)
        {
            await mediaStorageService.DeleteAsync(visualAsset.PublicId, cancellationToken);
        }

        var now = DateTime.UtcNow;
        visualAsset.IsDeleted = true;
        visualAsset.DeletedAt = now;
        visualAsset.DeletedBy = currentUserService.UserId;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}

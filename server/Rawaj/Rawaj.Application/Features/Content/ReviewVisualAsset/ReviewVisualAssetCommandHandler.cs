using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Content.ReviewVisualAsset;

public class ReviewVisualAssetCommandHandler(IApplicationDbContext dbContext, ICurrentTenantContext currentTenantContext)
    : IRequestHandler<ReviewVisualAssetCommand, Result<ReviewVisualAssetResponse>>
{
    public async Task<Result<ReviewVisualAssetResponse>> Handle(ReviewVisualAssetCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;

        var visualAsset = await dbContext.VisualAssets
            .FirstOrDefaultAsync(a => a.Id == request.VisualAssetId && a.TenantId == tenantId, cancellationToken);
        if (visualAsset is null)
        {
            return Result<ReviewVisualAssetResponse>.Failure("Visual asset not found.");
        }

        visualAsset.IsApproved = request.Approve;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<ReviewVisualAssetResponse>.Success(new ReviewVisualAssetResponse(visualAsset.Id, visualAsset.IsApproved));
    }
}

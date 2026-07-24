using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Content.ReviewContentItem;

public class ReviewContentItemCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    ICurrentTenantContext currentTenantContext)
    : IRequestHandler<ReviewContentItemCommand, Result<ReviewContentItemResponse>>
{
    public async Task<Result<ReviewContentItemResponse>> Handle(
        ReviewContentItemCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;

        var contentItem = await dbContext.ContentItems
            .FirstOrDefaultAsync(c => c.Id == request.ContentItemId && c.TenantId == tenantId, cancellationToken);
        if (contentItem is null)
        {
            return Result<ReviewContentItemResponse>.Failure("Content item not found.");
        }

        if (contentItem.Status == ContentStatus.Published)
        {
            return Result<ReviewContentItemResponse>.Failure("Published content cannot be reviewed.");
        }

        var now = DateTime.UtcNow;

        contentItem.Status = request.Approve ? ContentStatus.Approved : ContentStatus.Rejected;
        contentItem.ReviewedBy = currentUserService.UserId;
        contentItem.ReviewedAt = now;
        contentItem.UpdatedAt = now;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<ReviewContentItemResponse>.Success(
            new ReviewContentItemResponse(contentItem.Id, contentItem.Status, now));
    }
}

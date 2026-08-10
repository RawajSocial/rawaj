using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Scheduling.Common;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Content.ReviewContentItem;

public class ReviewContentItemCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    ICurrentTenantContext currentTenantContext,
    ITokenEncryptor tokenEncryptor,
    IEnumerable<ISocialPublisher> publishers)
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

        // Declining a post that's already scheduled must not leave it orphaned — silently still live
        // (or about to go live) on the platform while our own records call it rejected. A post that
        // has genuinely already gone out can't be undone here (mirrors CancelScheduledPost's own
        // "only Pending" rule) — reject the decline itself rather than let our state and reality
        // disagree.
        if (!request.Approve)
        {
            var activeScheduledPost = await dbContext.ScheduledPosts
                .FirstOrDefaultAsync(
                    s => s.ContentItemId == contentItem.Id
                        && (s.Status == ScheduledPostStatus.Pending || s.Status == ScheduledPostStatus.Published),
                    cancellationToken);

            if (activeScheduledPost is { Status: ScheduledPostStatus.Published })
            {
                return Result<ReviewContentItemResponse>.Failure(
                    "This post has already been published and cannot be declined.");
            }

            if (activeScheduledPost is not null)
            {
                var cancelNative = await ScheduledPostPublisher.CancelNativeAsync(
                    dbContext, tokenEncryptor, publishers, activeScheduledPost, cancellationToken);
                if (!cancelNative.Succeeded)
                {
                    return Result<ReviewContentItemResponse>.Failure(cancelNative.ErrorMessage!);
                }

                activeScheduledPost.Status = ScheduledPostStatus.Cancelled;
                activeScheduledPost.UpdatedAt = DateTime.UtcNow;
            }
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

using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Scheduling.Common;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Scheduling.TakeDownScheduledPost;

public class TakeDownScheduledPostCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentTenantContext currentTenantContext,
    ITokenEncryptor tokenEncryptor,
    IEnumerable<ISocialPublisher> publishers)
    : IRequestHandler<TakeDownScheduledPostCommand, Result<TakeDownScheduledPostResponse>>
{
    public async Task<Result<TakeDownScheduledPostResponse>> Handle(
        TakeDownScheduledPostCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;

        var scheduledPost = await dbContext.ScheduledPosts
            .FirstOrDefaultAsync(
                s => s.Id == request.ScheduledPostId && s.ContentItem.TenantId == tenantId,
                cancellationToken);
        if (scheduledPost is null)
        {
            return Result<TakeDownScheduledPostResponse>.Failure("Scheduled post not found.");
        }

        if (scheduledPost.Status != ScheduledPostStatus.Published)
        {
            return Result<TakeDownScheduledPostResponse>.Failure("Only a post that is actually live can be taken down.");
        }

        var takeDown = await ScheduledPostPublisher.TakeDownAsync(
            dbContext, tokenEncryptor, publishers, scheduledPost, cancellationToken);
        if (!takeDown.Succeeded)
        {
            return Result<TakeDownScheduledPostResponse>.Failure(takeDown.ErrorMessage!);
        }

        var now = DateTime.UtcNow;

        scheduledPost.Status = ScheduledPostStatus.TakenDown;
        scheduledPost.UpdatedAt = now;

        var contentItem = await dbContext.ContentItems
            .FirstAsync(c => c.Id == scheduledPost.ContentItemId, cancellationToken);
        contentItem.Status = ContentStatus.Draft;
        contentItem.ReviewedBy = null;
        contentItem.ReviewedAt = null;
        contentItem.UpdatedAt = now;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<TakeDownScheduledPostResponse>.Success(
            new TakeDownScheduledPostResponse(scheduledPost.Id, scheduledPost.Status));
    }
}

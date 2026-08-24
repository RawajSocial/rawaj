using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Scheduling.Common;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Scheduling.CancelScheduledPost;

public class CancelScheduledPostCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentTenantContext currentTenantContext,
    ITokenEncryptor tokenEncryptor,
    IEnumerable<ISocialPublisher> publishers)
    : IRequestHandler<CancelScheduledPostCommand, Result<CancelScheduledPostResponse>>
{
    public async Task<Result<CancelScheduledPostResponse>> Handle(
        CancelScheduledPostCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;

        var scheduledPost = await dbContext.ScheduledPosts
            .FirstOrDefaultAsync(
                s => s.Id == request.ScheduledPostId && s.ContentItem.TenantId == tenantId,
                cancellationToken);
        if (scheduledPost is null)
        {
            return Result<CancelScheduledPostResponse>.Failure("Scheduled post not found.");
        }

        if (scheduledPost.Status != ScheduledPostStatus.Pending)
        {
            return Result<CancelScheduledPostResponse>.Failure("Only pending scheduled posts can be cancelled.");
        }

        var cancelNative = await ScheduledPostPublisher.CancelNativeAsync(
            dbContext, tokenEncryptor, publishers, scheduledPost, cancellationToken);
        if (!cancelNative.Succeeded)
        {
            return Result<CancelScheduledPostResponse>.Failure(cancelNative.ErrorMessage!);
        }

        scheduledPost.Status = ScheduledPostStatus.Cancelled;
        scheduledPost.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<CancelScheduledPostResponse>.Success(
            new CancelScheduledPostResponse(scheduledPost.Id, scheduledPost.Status));
    }
}

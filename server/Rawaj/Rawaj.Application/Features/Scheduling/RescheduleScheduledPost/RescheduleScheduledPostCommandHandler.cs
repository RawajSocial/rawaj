using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Scheduling.Common;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Scheduling.RescheduleScheduledPost;

/// <summary>
/// Only a Pending post can move. A natively-scheduled post (PostId already set on the platform)
/// can't be "moved" via the platform API here, so this cancels the existing platform-side handoff
/// first (see ScheduledPostPublisher.CancelNativeAsync — skipping this would leave the old post
/// live and publish it a second time alongside the new one), then re-issues at the new time. No
/// coins are charged - scheduling was already paid for once.
/// </summary>
public class RescheduleScheduledPostCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentTenantContext currentTenantContext,
    ITokenEncryptor tokenEncryptor,
    IEnumerable<ISocialPublisher> publishers)
    : IRequestHandler<RescheduleScheduledPostCommand, Result<RescheduleScheduledPostResponse>>
{
    public async Task<Result<RescheduleScheduledPostResponse>> Handle(
        RescheduleScheduledPostCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;

        var scheduledPost = await dbContext.ScheduledPosts
            .FirstOrDefaultAsync(
                s => s.Id == request.ScheduledPostId && s.ContentItem.TenantId == tenantId,
                cancellationToken);
        if (scheduledPost is null)
        {
            return Result<RescheduleScheduledPostResponse>.Failure("Scheduled post not found.");
        }

        if (scheduledPost.Status != ScheduledPostStatus.Pending)
        {
            return Result<RescheduleScheduledPostResponse>.Failure("Only pending scheduled posts can be rescheduled.");
        }

        var cancelNative = await ScheduledPostPublisher.CancelNativeAsync(
            dbContext, tokenEncryptor, publishers, scheduledPost, cancellationToken);
        if (!cancelNative.Succeeded)
        {
            return Result<RescheduleScheduledPostResponse>.Failure(cancelNative.ErrorMessage!);
        }

        scheduledPost.ScheduledAt = request.ScheduledAt;
        scheduledPost.AiSuggestedTime = false;
        scheduledPost.PostId = null;
        scheduledPost.ErrorMessage = null;
        scheduledPost.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        var handoffResult = await ScheduledPostPublisher.ScheduleNativelyAsync(
            dbContext, tokenEncryptor, publishers, scheduledPost.Id, request.ScheduledAt, cancellationToken);

        if (!handoffResult.Succeeded)
        {
            return Result<RescheduleScheduledPostResponse>.Failure(handoffResult.ErrorMessage ?? "Could not reschedule this post.");
        }

        var post = handoffResult.Data!;
        return Result<RescheduleScheduledPostResponse>.Success(
            new RescheduleScheduledPostResponse(post.Id, post.ScheduledAt, post.Status));
    }
}

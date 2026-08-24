using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Scheduling.Common;

namespace Rawaj.Application.Features.Scheduling.PublishScheduledPost;

public class PublishScheduledPostCommandHandler(
    IApplicationDbContext dbContext,
    ITokenEncryptor tokenEncryptor,
    ICurrentTenantContext currentTenantContext,
    IEnumerable<ISocialPublisher> publishers)
    : IRequestHandler<PublishScheduledPostCommand, Result<PublishScheduledPostResponse>>
{
    public async Task<Result<PublishScheduledPostResponse>> Handle(
        PublishScheduledPostCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;

        var belongsToTenant = await dbContext.ScheduledPosts.AnyAsync(
            s => s.Id == request.ScheduledPostId && s.ContentItem.TenantId == tenantId, cancellationToken);
        if (!belongsToTenant)
        {
            return Result<PublishScheduledPostResponse>.Failure("Scheduled post not found.");
        }

        var result = await ScheduledPostPublisher.PublishNowAsync(
            dbContext, tokenEncryptor, publishers, request.ScheduledPostId, cancellationToken);

        if (!result.Succeeded)
        {
            return Result<PublishScheduledPostResponse>.Failure(result.ErrorMessage!);
        }

        var post = result.Data!;
        return Result<PublishScheduledPostResponse>.Success(
            new PublishScheduledPostResponse(post.Id, post.Status, post.PostId));
    }
}

using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Scheduling.RescheduleScheduledPost;

/// <summary>
/// Moves a pending scheduled post to a new time. Content is owned by the ContentItem (edited via
/// regenerate) - this only ever touches ScheduledAt, so there is no "content" field here.
/// </summary>
public record RescheduleScheduledPostCommand(Guid ScheduledPostId, DateTime ScheduledAt)
    : IRequest<Result<RescheduleScheduledPostResponse>>, IRequireTenantRole, IRequireResolvedBrandAccess
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Editor;

    public Task<Guid?> ResolveBrandProfileIdAsync(IApplicationDbContext dbContext, CancellationToken cancellationToken) =>
        dbContext.ScheduledPosts.Where(s => s.Id == ScheduledPostId).Select(s => (Guid?)s.SocialAccount.BrandProfileId).FirstOrDefaultAsync(cancellationToken);
}

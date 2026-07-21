using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Scheduling.CancelScheduledPost;

public record CancelScheduledPostCommand(Guid ScheduledPostId)
    : IRequest<Result<CancelScheduledPostResponse>>, IRequireTenantRole, IRequireResolvedBrandAccess
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Editor;

    public Task<Guid?> ResolveBrandProfileIdAsync(IApplicationDbContext dbContext, CancellationToken cancellationToken) =>
        dbContext.ScheduledPosts.Where(s => s.Id == ScheduledPostId).Select(s => (Guid?)s.SocialAccount.BrandProfileId).FirstOrDefaultAsync(cancellationToken);
}

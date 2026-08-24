using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Analytics.GetPostAnalytics;

public record GetPostAnalyticsQuery(Guid ScheduledPostId)
    : IRequest<Result<List<PostAnalyticsSnapshot>>>, IRequireTenantRole, IRequireResolvedBrandAccess
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Viewer;

    public Task<Guid?> ResolveBrandProfileIdAsync(IApplicationDbContext dbContext, CancellationToken cancellationToken) =>
        dbContext.ScheduledPosts.Where(s => s.Id == ScheduledPostId).Select(s => (Guid?)s.SocialAccount.BrandProfileId).FirstOrDefaultAsync(cancellationToken);
}

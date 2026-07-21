using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Analytics.SyncPostAnalytics;

public record SyncPostAnalyticsCommand(Guid ScheduledPostId)
    : IRequest<Result<PostAnalyticsSnapshot>>, IRequireTenantRole, IRequireResolvedBrandAccess
{
    // Write operation (triggers external API calls, inserts a PostAnalytics row) - was previously
    // Viewer, which is wrong for a mutating action.
    public TenantMemberRole MinimumRole => TenantMemberRole.Editor;

    public Task<Guid?> ResolveBrandProfileIdAsync(IApplicationDbContext dbContext, CancellationToken cancellationToken) =>
        dbContext.ScheduledPosts.Where(s => s.Id == ScheduledPostId).Select(s => (Guid?)s.SocialAccount.BrandProfileId).FirstOrDefaultAsync(cancellationToken);
}

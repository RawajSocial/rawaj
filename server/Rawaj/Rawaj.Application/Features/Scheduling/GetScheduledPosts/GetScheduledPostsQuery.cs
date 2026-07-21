using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Scheduling.GetScheduledPosts;

public record GetScheduledPostsQuery(Guid? CampaignId, int Page = 1, int PageSize = 20)
    : IRequest<Result<PagedResult<ScheduledPostSummary>>>, IRequireTenantRole, IRequireResolvedBrandAccess
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Viewer;

    // When CampaignId is omitted there's nothing to check here - the handler restricts an
    // Editor/Viewer caller to their accessible brands itself in that case.
    public Task<Guid?> ResolveBrandProfileIdAsync(IApplicationDbContext dbContext, CancellationToken cancellationToken) =>
        CampaignId is null
            ? Task.FromResult<Guid?>(null)
            : dbContext.MarketingCampaigns.Where(c => c.Id == CampaignId).Select(c => (Guid?)c.BrandProfileId).FirstOrDefaultAsync(cancellationToken);
}

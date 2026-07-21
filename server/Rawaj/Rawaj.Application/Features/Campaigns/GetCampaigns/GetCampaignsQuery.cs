using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Campaigns.GetCampaigns;

public record GetCampaignsQuery(Guid? BrandProfileId, int Page = 1, int PageSize = 20)
    : IRequest<Result<PagedResult<CampaignSummary>>>, IRequireTenantRole, IRequireResolvedBrandAccess
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Viewer;

    // When BrandProfileId is omitted, there's nothing to check here - the handler restricts an
    // Editor/Viewer caller to their accessible brands itself in that case.
    public Task<Guid?> ResolveBrandProfileIdAsync(IApplicationDbContext dbContext, CancellationToken cancellationToken) =>
        Task.FromResult(BrandProfileId);
}

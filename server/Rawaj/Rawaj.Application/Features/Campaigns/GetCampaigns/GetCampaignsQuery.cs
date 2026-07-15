using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Campaigns.GetCampaigns;

public record GetCampaignsQuery(Guid? BrandProfileId, int Page = 1, int PageSize = 20)
    : IRequest<Result<PagedResult<CampaignSummary>>>, IRequireTenantRole
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Viewer;
}

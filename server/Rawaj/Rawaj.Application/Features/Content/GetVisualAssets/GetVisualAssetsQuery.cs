using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Content.GetVisualAssets;

public record GetVisualAssetsQuery(Guid CampaignId, int Page = 1, int PageSize = 20)
    : IRequest<Result<PagedResult<VisualAssetSummary>>>, IRequireTenantRole
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Viewer;
}

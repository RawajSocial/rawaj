using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Content.GetContentItems;

public record GetContentItemsQuery(Guid CampaignId, int Page = 1, int PageSize = 20)
    : IRequest<Result<PagedResult<ContentItemSummary>>>, IRequireTenantRole
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Viewer;
}

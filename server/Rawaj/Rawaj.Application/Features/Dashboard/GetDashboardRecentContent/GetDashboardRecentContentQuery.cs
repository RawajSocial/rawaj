using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Content.GetContentItems;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Dashboard.GetDashboardRecentContent;

public record GetDashboardRecentContentQuery(Guid BrandProfileId, Guid? CampaignId, int Take = 10)
    : IRequest<Result<List<ContentItemSummary>>>, IRequireTenantRole, IRequireBrandAccess
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Viewer;
}

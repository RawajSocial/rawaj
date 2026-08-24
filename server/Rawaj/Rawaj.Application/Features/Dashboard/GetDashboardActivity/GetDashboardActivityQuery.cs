using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Dashboard.GetDashboardActivity;

public record GetDashboardActivityQuery(Guid BrandProfileId, Guid? CampaignId, int Take = 20)
    : IRequest<Result<List<DashboardActivityItem>>>, IRequireTenantRole, IRequireBrandAccess
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Viewer;
}

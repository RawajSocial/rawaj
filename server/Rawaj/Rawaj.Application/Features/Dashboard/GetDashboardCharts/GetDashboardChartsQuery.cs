using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Dashboard.GetDashboardCharts;

public record GetDashboardChartsQuery(Guid BrandProfileId, Guid? CampaignId, int Days = 14)
    : IRequest<Result<DashboardChartsResponse>>, IRequireTenantRole, IRequireBrandAccess
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Viewer;
}

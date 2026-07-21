using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Analytics.GetBrandAnalytics;

public record GetBrandAnalyticsQuery(Guid BrandProfileId)
    : IRequest<Result<BrandAnalyticsOverview>>, IRequireTenantRole, IRequireBrandAccess
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Viewer;
}

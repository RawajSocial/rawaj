using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Analytics.GetCampaignAnalytics;

public record GetCampaignAnalyticsQuery(Guid CampaignId)
    : IRequest<Result<CampaignAnalyticsSummary>>, IRequireTenantRole
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Viewer;
}

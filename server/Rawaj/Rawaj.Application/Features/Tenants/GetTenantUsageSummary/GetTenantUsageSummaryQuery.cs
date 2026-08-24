using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Tenants.GetTenantUsageSummary;

public record GetTenantUsageSummaryQuery : IRequest<Result<TenantUsageSummaryResponse>>, IRequireTenantRole
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Admin;
}

public record TenantUsageSummaryResponse(
    int ContentItemsGenerated,
    int VisualAssetsGenerated,
    int CampaignsCreated,
    int PostsPublished,
    int CoinsSpentAllTime,
    int CoinsSpentLast30Days);

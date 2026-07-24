using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Billing.GetCoinPricing;

/// <summary>
/// The one source of coin/purchase pricing the frontend reads from — both the public pricing page
/// and the per-button spend-preview captions use this, so displayed numbers can never drift from
/// what will actually be charged. Tenant-scoped because the discounted costs and remaining
/// free-trial counters are per-tenant.
/// </summary>
public record GetCoinPricingQuery : IRequest<Result<GetCoinPricingResponse>>, IRequireTenantRole
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Viewer;
}

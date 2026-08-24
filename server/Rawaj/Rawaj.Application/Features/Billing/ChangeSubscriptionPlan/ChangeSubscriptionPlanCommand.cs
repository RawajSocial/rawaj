using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Billing.ChangeSubscriptionPlan;

/// <summary>
/// The one real "subscribe/upgrade/downgrade" endpoint — a fake payment (no gateway call, no card
/// validation, always succeeds) that swaps the tenant's subscription to <paramref name="SubscriptionPlanId"/>,
/// records a <c>BillingTransaction</c>, and — the first time a tenant leaves the Free plan — flips
/// it to <see cref="Rawaj.Domain.Enums.TenantType.Agency"/>. <paramref name="AgencySize"/>/
/// <paramref name="ServicesOffered"/> are only required for that first paid subscription; omit them
/// on any later plan change.
/// </summary>
public record ChangeSubscriptionPlanCommand(
    Guid SubscriptionPlanId,
    string? AgencySize = null,
    List<string>? ServicesOffered = null)
    : IRequest<Result<ChangeSubscriptionPlanResponse>>, IRequireTenantRole
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Owner;
}

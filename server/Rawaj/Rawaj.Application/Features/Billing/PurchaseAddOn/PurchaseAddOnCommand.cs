using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Billing.PurchaseAddOn;

/// <summary>
/// Buys one unit of an add-on (extra brand slot or extra marketeer seat), raising the tenant's
/// effective limit on top of its plan's base allowance. Fake payment, and — since there is no
/// recurring billing/webhook infrastructure yet — a one-time "unlock" rather than a real monthly
/// charge; a future gateway integration would need to re-bill this every period.
/// </summary>
public record PurchaseAddOnCommand(AddOnType Type)
    : IRequest<Result<PurchaseAddOnResponse>>, IRequireTenantRole
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Owner;
}

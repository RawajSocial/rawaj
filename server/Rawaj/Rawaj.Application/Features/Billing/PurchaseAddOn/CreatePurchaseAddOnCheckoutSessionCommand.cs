using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Billing.Common;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Billing.PurchaseAddOn;

/// <summary>
/// Starts a real Stripe Checkout payment for one unit of an add-on (extra brand slot or extra
/// marketeer seat). Since there's no recurring billing wired up for add-ons yet, this remains a
/// one-time "unlock" rather than a real monthly charge — a future change would need to re-bill this
/// every period. The counter is bumped by the webhook once payment is confirmed, not here — see
/// <see cref="IBillingFulfillmentService.FulfillAddOnPurchaseAsync"/>.
/// </summary>
public record CreatePurchaseAddOnCheckoutSessionCommand(AddOnType Type)
    : IRequest<Result<CreateCheckoutSessionResponse>>, IRequireTenantRole
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Owner;
}

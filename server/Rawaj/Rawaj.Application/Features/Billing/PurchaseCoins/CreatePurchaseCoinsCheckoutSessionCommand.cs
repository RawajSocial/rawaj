using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Billing.Common;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Billing.PurchaseCoins;

/// <summary>
/// Starts a real Stripe Checkout payment for a fixed coin package (<paramref name="CoinPackageId"/>)
/// or a custom amount (<paramref name="CustomCoins"/>, priced at
/// <see cref="Common.BillingPricing.CustomCoinPricePerCoin"/>) — exactly one of the two must be
/// provided. Coins are granted from the Stripe webhook once payment is confirmed, not from this
/// command — see <see cref="IBillingFulfillmentService.FulfillCoinPurchaseAsync"/>.
/// </summary>
public record CreatePurchaseCoinsCheckoutSessionCommand(Guid? CoinPackageId, int? CustomCoins)
    : IRequest<Result<CreateCheckoutSessionResponse>>, IRequireTenantRole
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Admin;
}

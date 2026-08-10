namespace Rawaj.Application.Features.Billing.Common;

/// <summary>Flat prices not tied to a DB row — coin packages/subscription plans carry their own
/// price, but a custom coin amount and the two add-ons don't. Single source of truth referenced by
/// both the checkout-session commands (that charge these amounts) and the pricing-display queries
/// (<c>GetCoinPricingQueryHandler</c>/<c>GetPublicCoinPricingQueryHandler</c>) that surface them.</summary>
public static class BillingPricing
{
    public const decimal CustomCoinPricePerCoin = 0.01m;
    public const decimal ExtraBrandPriceUsd = 5m;
    public const decimal ExtraMarketeerPriceUsd = 3m;
}

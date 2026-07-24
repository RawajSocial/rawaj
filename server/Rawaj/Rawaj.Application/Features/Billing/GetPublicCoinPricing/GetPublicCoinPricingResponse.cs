using Rawaj.Application.Features.Billing.GetCoinPricing;

namespace Rawaj.Application.Features.Billing.GetPublicCoinPricing;

public record GetPublicCoinPricingResponse(
    CoinPriceList BaseCosts,
    List<CoinPackageSummary> CoinPackages,
    decimal CustomCoinPricePerCoin,
    decimal ExtraBrandPriceUsd,
    decimal ExtraMarketeerPriceUsd);

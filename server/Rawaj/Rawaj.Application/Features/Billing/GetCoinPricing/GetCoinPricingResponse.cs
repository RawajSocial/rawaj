namespace Rawaj.Application.Features.Billing.GetCoinPricing;

public record CoinPriceList(
    int ContentGeneration,
    int VisualGeneration,
    int CampaignContentGeneration,
    int MarketingPlanGeneration,
    int Scheduling,
    /// <summary>Reserved pricing only — no feature spends this yet.</summary>
    int BusinessDiagnosis,
    /// <summary>Reserved pricing only — no feature spends this yet.</summary>
    int CompetitiveAnalysis,
    /// <summary>Reserved pricing only — no feature spends this yet.</summary>
    int ReasoningConversation);

public record CoinPackageSummary(Guid CoinPackageId, string Name, int Coins, int BonusCoins, decimal PriceUsd);

public record GetCoinPricingResponse(
    CoinPriceList BaseCosts,
    CoinPriceList DiscountedCosts,
    int DiscountPercent,
    bool FreeMarketingPlanAvailable,
    int FreeImageGenerationsRemaining,
    int FreeContentGenerationsRemaining,
    List<CoinPackageSummary> CoinPackages,
    decimal CustomCoinPricePerCoin,
    decimal ExtraBrandPriceUsd,
    decimal ExtraMarketeerPriceUsd);

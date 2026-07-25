using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Billing.GetCoinPricing;
using Rawaj.Application.Features.Billing.PurchaseAddOn;
using Rawaj.Application.Features.Billing.PurchaseCoins;

namespace Rawaj.Application.Features.Billing.GetPublicCoinPricing;

public class GetPublicCoinPricingQueryHandler(IApplicationDbContext dbContext, ICoinCostProvider coinCostProvider, IAppCache cache)
    : IRequestHandler<GetPublicCoinPricingQuery, Result<GetPublicCoinPricingResponse>>
{
    public const string CacheKey = "public-coin-pricing";

    public async Task<Result<GetPublicCoinPricingResponse>> Handle(
        GetPublicCoinPricingQuery request, CancellationToken cancellationToken)
    {
        var response = await cache.GetOrCreateAsync(CacheKey, TimeSpan.FromMinutes(15), async () =>
        {
            var baseCosts = new CoinPriceList(
                coinCostProvider.ContentGeneration,
                coinCostProvider.VisualGeneration,
                coinCostProvider.CampaignContentGeneration,
                coinCostProvider.MarketingPlanGeneration,
                coinCostProvider.Scheduling,
                coinCostProvider.BusinessDiagnosis,
                coinCostProvider.CompetitiveAnalysis,
                coinCostProvider.ReasoningConversation);

            var coinPackages = await dbContext.CoinPackages
                .Where(p => p.IsActive)
                .OrderBy(p => p.PriceUsd)
                .Select(p => new CoinPackageSummary(p.Id, p.Name, p.Coins, p.BonusCoins, p.PriceUsd))
                .ToListAsync(cancellationToken);

            return new GetPublicCoinPricingResponse(
                baseCosts,
                coinPackages,
                PurchaseCoinsCommandHandler.CustomPricePerCoin,
                PurchaseAddOnCommandHandler.ExtraBrandPriceUsd,
                PurchaseAddOnCommandHandler.ExtraMarketeerPriceUsd);
        });

        return Result<GetPublicCoinPricingResponse>.Success(response);
    }
}

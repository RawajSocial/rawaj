using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Policies;
using Rawaj.Application.Features.Billing.PurchaseAddOn;
using Rawaj.Application.Features.Billing.PurchaseCoins;

namespace Rawaj.Application.Features.Billing.GetCoinPricing;

public class GetCoinPricingQueryHandler(
    IApplicationDbContext dbContext, ICurrentTenantContext currentTenantContext, ICoinCostProvider coinCostProvider)
    : IRequestHandler<GetCoinPricingQuery, Result<GetCoinPricingResponse>>
{
    public async Task<Result<GetCoinPricingResponse>> Handle(GetCoinPricingQuery request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;

        var tenant = await dbContext.Tenants.FirstAsync(t => t.Id == tenantId, cancellationToken);

        var discountPercent = await (
            from t in dbContext.Tenants
            join subscription in dbContext.Subscriptions on t.SubscriptionId equals subscription.Id
            join plan in dbContext.SubscriptionPlans on subscription.SubscriptionPlanId equals plan.Id
            where t.Id == tenantId
            select plan.CoinUsageDiscountPercent
        ).FirstAsync(cancellationToken);

        var baseCosts = new CoinPriceList(
            coinCostProvider.ContentGeneration,
            coinCostProvider.VisualGeneration,
            coinCostProvider.CampaignContentGeneration,
            coinCostProvider.MarketingPlanGeneration,
            coinCostProvider.Scheduling,
            coinCostProvider.BusinessDiagnosis,
            coinCostProvider.CompetitiveAnalysis,
            coinCostProvider.ReasoningConversation);

        var discountedCosts = new CoinPriceList(
            await CoinPricingPolicy.GetDiscountedCostAsync(dbContext, tenantId, baseCosts.ContentGeneration, cancellationToken),
            await CoinPricingPolicy.GetDiscountedCostAsync(dbContext, tenantId, baseCosts.VisualGeneration, cancellationToken),
            await CoinPricingPolicy.GetDiscountedCostAsync(dbContext, tenantId, baseCosts.CampaignContentGeneration, cancellationToken),
            await CoinPricingPolicy.GetDiscountedCostAsync(dbContext, tenantId, baseCosts.MarketingPlanGeneration, cancellationToken),
            await CoinPricingPolicy.GetDiscountedCostAsync(dbContext, tenantId, baseCosts.Scheduling, cancellationToken),
            baseCosts.BusinessDiagnosis,
            baseCosts.CompetitiveAnalysis,
            baseCosts.ReasoningConversation);

        var coinPackages = await dbContext.CoinPackages
            .Where(p => p.IsActive)
            .OrderBy(p => p.PriceUsd)
            .Select(p => new CoinPackageSummary(p.Id, p.Name, p.Coins, p.BonusCoins, p.PriceUsd))
            .ToListAsync(cancellationToken);

        return Result<GetCoinPricingResponse>.Success(new GetCoinPricingResponse(
            baseCosts,
            discountedCosts,
            discountPercent,
            !tenant.FreeMarketingPlanUsed,
            tenant.FreeImageGenerationsRemaining,
            tenant.FreeContentGenerationsRemaining,
            coinPackages,
            PurchaseCoinsCommandHandler.CustomPricePerCoin,
            PurchaseAddOnCommandHandler.ExtraBrandPriceUsd,
            PurchaseAddOnCommandHandler.ExtraMarketeerPriceUsd));
    }
}

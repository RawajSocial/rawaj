using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;

namespace Rawaj.Application.Common.Policies;

/// <summary>
/// Applies the tenant's active <c>SubscriptionPlan.CoinUsageDiscountPercent</c> to a base coin
/// cost from <see cref="ICoinCostProvider"/>. Every coin-spending handler calls this once to get
/// the real, discounted cost before checking/debiting a balance via <see cref="CoinPolicy"/> —
/// keeping the discount math in one place instead of repeated per handler.
/// </summary>
public static class CoinPricingPolicy
{
    public static async Task<int> GetDiscountedCostAsync(
        IApplicationDbContext dbContext, Guid tenantId, int baseCost, CancellationToken cancellationToken)
    {
        var discountPercent = await (
            from tenant in dbContext.Tenants
            join subscription in dbContext.Subscriptions on tenant.SubscriptionId equals subscription.Id
            join plan in dbContext.SubscriptionPlans on subscription.SubscriptionPlanId equals plan.Id
            where tenant.Id == tenantId
            select plan.CoinUsageDiscountPercent
        ).FirstAsync(cancellationToken);

        if (discountPercent <= 0)
        {
            return baseCost;
        }

        return (int)Math.Ceiling(baseCost * (1 - discountPercent / 100.0));
    }
}

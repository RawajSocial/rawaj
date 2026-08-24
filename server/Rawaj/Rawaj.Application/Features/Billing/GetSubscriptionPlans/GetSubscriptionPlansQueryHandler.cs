using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Billing.GetSubscriptionPlans;

public class GetSubscriptionPlansQueryHandler(IApplicationDbContext dbContext, IAppCache cache)
    : IRequestHandler<GetSubscriptionPlansQuery, Result<List<SubscriptionPlanSummary>>>
{
    public const string CacheKey = "subscription-plans";

    public async Task<Result<List<SubscriptionPlanSummary>>> Handle(GetSubscriptionPlansQuery request, CancellationToken cancellationToken)
    {
        var plans = await cache.GetOrCreateAsync(CacheKey, TimeSpan.FromMinutes(15), () =>
            dbContext.SubscriptionPlans
                .Where(p => p.IsActive)
                .OrderBy(p => p.Cost)
                .Select(p => new SubscriptionPlanSummary(
                    p.Id,
                    p.Name,
                    p.Cost,
                    p.BillingCycle,
                    p.Currency,
                    p.MaxBrands,
                    p.MaxUsers,
                    p.MaxCampaignsMonthly,
                    p.MaxAiCreditsMonthly,
                    p.MaxScheduledPosts,
                    p.MaxSocialAccounts,
                    p.CoinUsageDiscountPercent,
                    p.MonthlyCoinGrant,
                    p.Features))
                .ToListAsync(cancellationToken));

        return Result<List<SubscriptionPlanSummary>>.Success(plans);
    }
}

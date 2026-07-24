using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Billing.GetSubscription;

public class GetSubscriptionQueryHandler(IApplicationDbContext dbContext, ICurrentTenantContext currentTenantContext)
    : IRequestHandler<GetSubscriptionQuery, Result<GetSubscriptionResponse>>
{
    public async Task<Result<GetSubscriptionResponse>> Handle(GetSubscriptionQuery request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;

        var response = await (
            from tenant in dbContext.Tenants
            join subscription in dbContext.Subscriptions on tenant.SubscriptionId equals subscription.Id
            join plan in dbContext.SubscriptionPlans on subscription.SubscriptionPlanId equals plan.Id
            where tenant.Id == tenantId
            select new GetSubscriptionResponse(
                subscription.Id,
                plan.Id,
                plan.Name,
                plan.Cost,
                subscription.Status,
                subscription.BillingCycle,
                subscription.CurrentPeriodStart,
                subscription.CurrentPeriodEnd,
                subscription.TrialEndsAt)
        ).FirstAsync(cancellationToken);

        return Result<GetSubscriptionResponse>.Success(response);
    }
}

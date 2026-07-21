using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Billing.ChangeSubscriptionPlan;

public class ChangeSubscriptionPlanCommandHandler(IApplicationDbContext dbContext, ICurrentTenantContext currentTenantContext)
    : IRequestHandler<ChangeSubscriptionPlanCommand, Result<ChangeSubscriptionPlanResponse>>
{
    public async Task<Result<ChangeSubscriptionPlanResponse>> Handle(
        ChangeSubscriptionPlanCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;

        var plan = await dbContext.SubscriptionPlans
            .FirstOrDefaultAsync(p => p.Id == request.SubscriptionPlanId && p.IsActive, cancellationToken);
        if (plan is null)
        {
            return Result<ChangeSubscriptionPlanResponse>.Failure("Subscription plan not found.");
        }

        var tenant = await dbContext.Tenants.FirstAsync(t => t.Id == tenantId, cancellationToken);
        var subscription = await dbContext.Subscriptions.FirstAsync(s => s.Id == tenant.SubscriptionId, cancellationToken);

        var now = DateTime.UtcNow;

        subscription.SubscriptionPlanId = plan.Id;
        subscription.BillingCycle = plan.BillingCycle;
        subscription.Status = SubscriptionStatus.Active;
        subscription.CurrentPeriodStart = now;
        subscription.CurrentPeriodEnd = plan.BillingCycle == BillingCycle.Yearly ? now.AddYears(1) : now.AddMonths(1);
        subscription.TrialEndsAt = null;
        subscription.CancelledAt = null;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<ChangeSubscriptionPlanResponse>.Success(
            new ChangeSubscriptionPlanResponse(
                subscription.Id,
                plan.Name,
                plan.Cost,
                subscription.Status,
                subscription.CurrentPeriodStart,
                subscription.CurrentPeriodEnd));
    }
}

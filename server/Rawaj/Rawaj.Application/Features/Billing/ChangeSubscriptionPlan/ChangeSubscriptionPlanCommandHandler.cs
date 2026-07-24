using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Entities.Billing;
using Rawaj.Domain.Enums;
using Rawaj.Domain.ValueObjects;

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

        // Capture the pre-change state before it's overwritten below, so the coin-grant decision
        // can tell "genuinely switching plans" (always grant) apart from "re-selecting the plan
        // you're already on" (grant only if this billing period hasn't been granted yet) — without
        // this, since CurrentPeriodStart always resets to `now` a few lines down, comparing against
        // the NEW period start would let re-selecting the same plan farm coins indefinitely.
        var previousPlanId = subscription.SubscriptionPlanId;
        var previousPeriodStart = subscription.CurrentPeriodStart;

        subscription.SubscriptionPlanId = plan.Id;
        subscription.BillingCycle = plan.BillingCycle;
        subscription.Status = SubscriptionStatus.Active;
        subscription.CurrentPeriodStart = now;
        subscription.CurrentPeriodEnd = plan.BillingCycle == BillingCycle.Yearly ? now.AddYears(1) : now.AddMonths(1);
        subscription.TrialEndsAt = null;
        subscription.CancelledAt = null;

        var shouldGrantCoins = plan.MonthlyCoinGrant > 0 && (
            previousPlanId != plan.Id
            || subscription.LastCoinGrantAt is null
            || subscription.LastCoinGrantAt < previousPeriodStart);

        if (shouldGrantCoins)
        {
            tenant.CoinBalance += plan.MonthlyCoinGrant;
            subscription.LastCoinGrantAt = now;

            dbContext.BillingTransactions.Add(new BillingTransaction
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Type = BillingTransactionType.CoinGrant,
                Description = $"{plan.Name} plan coin grant",
                AmountUsd = 0,
                CoinsGranted = plan.MonthlyCoinGrant,
                CreatedAt = now
            });
        }

        // Only the Free plan is a "business owner" per the pricing sheet — any paid plan implies
        // the tenant is now operating as a marketing agency.
        if (plan.Cost > 0 && tenant.TenantType == TenantType.Business)
        {
            tenant.TenantType = TenantType.Agency;
            if (request.AgencySize is not null || request.ServicesOffered is not null)
            {
                var profile = tenant.TenantProfile ?? new TenantProfile();
                profile.AgencySize = request.AgencySize ?? profile.AgencySize;
                profile.ServicesOffered = request.ServicesOffered ?? profile.ServicesOffered;
                tenant.TenantProfile = profile;
            }
        }

        tenant.UpdatedAt = now;

        // Fake payment: no gateway call, no card validation, always succeeds. This is a placeholder
        // until a real payment integration exists (see the unused Stripe fields on Subscription).
        dbContext.BillingTransactions.Add(new BillingTransaction
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Type = BillingTransactionType.PlanChange,
            Description = $"Subscribed to the {plan.Name} plan",
            AmountUsd = plan.Cost,
            CreatedAt = now
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<ChangeSubscriptionPlanResponse>.Success(
            new ChangeSubscriptionPlanResponse(
                subscription.Id,
                plan.Name,
                plan.Cost,
                subscription.Status,
                subscription.CurrentPeriodStart,
                subscription.CurrentPeriodEnd,
                tenant.TenantType,
                tenant.CoinBalance,
                shouldGrantCoins ? plan.MonthlyCoinGrant : 0));
    }
}

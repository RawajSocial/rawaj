using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Features.Billing.ChangeSubscriptionPlan;
using Rawaj.Application.Features.Billing.PurchaseAddOn;
using Rawaj.Application.Features.Billing.PurchaseCoins;
using Rawaj.Domain.Entities.Billing;
using Rawaj.Domain.Enums;
using Rawaj.Domain.ValueObjects;

namespace Rawaj.Application.Features.Billing.Common;

public class BillingFulfillmentService(IApplicationDbContext dbContext) : IBillingFulfillmentService
{
    public async Task<bool> IsEventAlreadyProcessedAsync(string stripeEventId, CancellationToken cancellationToken) =>
        await dbContext.BillingTransactions.AnyAsync(t => t.StripeEventId == stripeEventId, cancellationToken);

    public async Task<bool> IsSessionAlreadyProcessedAsync(string stripeSessionId, CancellationToken cancellationToken) =>
        await dbContext.BillingTransactions.AnyAsync(t => t.StripeSessionId == stripeSessionId, cancellationToken);

    public async Task<PurchaseCoinsResponse> FulfillCoinPurchaseAsync(
        Guid tenantId, Guid? coinPackageId, int? customCoins,
        string? stripeSessionId, string? stripeEventId, CancellationToken cancellationToken)
    {
        int coinsGranted;
        decimal amountUsd;
        string description;

        if (coinPackageId.HasValue)
        {
            var package = await dbContext.CoinPackages
                .FirstOrDefaultAsync(p => p.Id == coinPackageId.Value && p.IsActive, cancellationToken)
                ?? throw new InvalidOperationException("Coin package not found.");

            coinsGranted = package.Coins + package.BonusCoins;
            amountUsd = package.PriceUsd;
            description = $"Purchased the {package.Name} coin package ({coinsGranted} coins)";
        }
        else
        {
            coinsGranted = customCoins!.Value;
            amountUsd = coinsGranted * BillingPricing.CustomCoinPricePerCoin;
            description = $"Purchased {coinsGranted} coins (custom amount)";
        }

        var tenant = await dbContext.Tenants.FirstAsync(t => t.Id == tenantId, cancellationToken);
        var now = DateTime.UtcNow;

        tenant.CoinBalance += coinsGranted;
        tenant.UpdatedAt = now;

        var transaction = new BillingTransaction
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Type = BillingTransactionType.CoinPurchase,
            Description = description,
            AmountUsd = amountUsd,
            CoinsGranted = coinsGranted,
            CreatedAt = now,
            StripeSessionId = stripeSessionId,
            StripeEventId = stripeEventId,
        };
        dbContext.BillingTransactions.Add(transaction);

        dbContext.CoinLedgerEntries.Add(new CoinLedgerEntry
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Amount = coinsGranted,
            Reason = "coin_purchase",
            ReferenceType = "billing_transaction",
            ReferenceId = transaction.Id,
            CreatedAt = now,
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return new PurchaseCoinsResponse(coinsGranted, amountUsd, tenant.CoinBalance);
    }

    public async Task<PurchaseAddOnResponse> FulfillAddOnPurchaseAsync(
        Guid tenantId, AddOnType type,
        string? stripeSessionId, string? stripeEventId, CancellationToken cancellationToken)
    {
        var tenant = await dbContext.Tenants.FirstAsync(t => t.Id == tenantId, cancellationToken);
        var now = DateTime.UtcNow;
        decimal amountUsd;
        string description;

        if (type == AddOnType.ExtraBrand)
        {
            tenant.ExtraBrandsPurchased++;
            amountUsd = BillingPricing.ExtraBrandPriceUsd;
            description = "Purchased an extra brand slot";
        }
        else
        {
            tenant.ExtraMarketeersPurchased++;
            amountUsd = BillingPricing.ExtraMarketeerPriceUsd;
            description = "Purchased an extra marketeer seat";
        }

        tenant.UpdatedAt = now;

        dbContext.BillingTransactions.Add(new BillingTransaction
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Type = BillingTransactionType.AddOnPurchase,
            Description = description,
            AmountUsd = amountUsd,
            CreatedAt = now,
            StripeSessionId = stripeSessionId,
            StripeEventId = stripeEventId,
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return new PurchaseAddOnResponse(type, amountUsd, tenant.ExtraBrandsPurchased, tenant.ExtraMarketeersPurchased);
    }

    public async Task<ChangeSubscriptionPlanResponse> FulfillPlanChangeAsync(
        Guid tenantId, Guid subscriptionPlanId, string? agencySize, List<string>? servicesOffered,
        string? stripeCustomerId, string? stripeSubscriptionId,
        string? stripeSessionId, string? stripeEventId, CancellationToken cancellationToken)
    {
        var plan = await dbContext.SubscriptionPlans
            .FirstOrDefaultAsync(p => p.Id == subscriptionPlanId && p.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Subscription plan not found.");

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

        if (!string.IsNullOrWhiteSpace(stripeCustomerId)) subscription.StripeCustomerId = stripeCustomerId;
        if (!string.IsNullOrWhiteSpace(stripeSubscriptionId)) subscription.StripeSubscriptionId = stripeSubscriptionId;

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
                CreatedAt = now,
            });

            dbContext.CoinLedgerEntries.Add(new CoinLedgerEntry
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Amount = plan.MonthlyCoinGrant,
                Reason = "plan_change_grant",
                ReferenceType = "subscription",
                ReferenceId = subscription.Id,
                CreatedAt = now,
            });
        }

        // Only the Free plan is a "business owner" per the pricing sheet — any paid plan implies
        // the tenant is now operating as a marketing agency.
        if (plan.Cost > 0 && tenant.TenantType == TenantType.Business)
        {
            tenant.TenantType = TenantType.Agency;
            if (agencySize is not null || servicesOffered is not null)
            {
                var profile = tenant.TenantProfile ?? new TenantProfile();
                profile.AgencySize = agencySize ?? profile.AgencySize;
                profile.ServicesOffered = servicesOffered ?? profile.ServicesOffered;
                tenant.TenantProfile = profile;
            }
        }

        tenant.UpdatedAt = now;

        dbContext.BillingTransactions.Add(new BillingTransaction
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Type = BillingTransactionType.PlanChange,
            Description = $"Subscribed to the {plan.Name} plan",
            AmountUsd = plan.Cost,
            CreatedAt = now,
            StripeSessionId = stripeSessionId,
            StripeEventId = stripeEventId,
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return new ChangeSubscriptionPlanResponse(
            subscription.Id,
            plan.Name,
            plan.Cost,
            subscription.Status,
            subscription.CurrentPeriodStart,
            subscription.CurrentPeriodEnd,
            tenant.TenantType,
            tenant.CoinBalance,
            shouldGrantCoins ? plan.MonthlyCoinGrant : 0);
    }

    public async Task GrantSubscriptionRenewalAsync(string stripeSubscriptionId, string stripeEventId, CancellationToken cancellationToken)
    {
        var subscription = await dbContext.Subscriptions
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscriptionId, cancellationToken);
        if (subscription is null) return;

        var tenant = await dbContext.Tenants.FirstOrDefaultAsync(t => t.SubscriptionId == subscription.Id, cancellationToken);
        if (tenant is null) return;

        var plan = await dbContext.SubscriptionPlans.FirstAsync(p => p.Id == subscription.SubscriptionPlanId, cancellationToken);
        var now = DateTime.UtcNow;

        subscription.Status = SubscriptionStatus.Active;
        subscription.CurrentPeriodStart = now;
        subscription.CurrentPeriodEnd = subscription.BillingCycle == BillingCycle.Yearly ? now.AddYears(1) : now.AddMonths(1);

        if (plan.MonthlyCoinGrant > 0)
        {
            tenant.CoinBalance += plan.MonthlyCoinGrant;
            subscription.LastCoinGrantAt = now;
            tenant.UpdatedAt = now;

            dbContext.BillingTransactions.Add(new BillingTransaction
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Type = BillingTransactionType.CoinGrant,
                Description = $"{plan.Name} plan coin grant (renewal)",
                AmountUsd = 0,
                CoinsGranted = plan.MonthlyCoinGrant,
                CreatedAt = now,
                StripeEventId = stripeEventId,
            });

            dbContext.CoinLedgerEntries.Add(new CoinLedgerEntry
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Amount = plan.MonthlyCoinGrant,
                Reason = "subscription_renewal_grant",
                ReferenceType = "subscription",
                ReferenceId = subscription.Id,
                CreatedAt = now,
            });
        }
        else
        {
            // No coins to grant, but the renewal itself still needs an idempotency record — otherwise
            // a redelivered invoice.paid for a zero-coin plan would re-advance the period every time.
            dbContext.BillingTransactions.Add(new BillingTransaction
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Type = BillingTransactionType.PlanChange,
                Description = $"{plan.Name} plan renewed",
                AmountUsd = plan.Cost,
                CreatedAt = now,
                StripeEventId = stripeEventId,
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateSubscriptionStatusAsync(string stripeSubscriptionId, SubscriptionStatus status, CancellationToken cancellationToken)
    {
        var subscription = await dbContext.Subscriptions
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscriptionId, cancellationToken);
        if (subscription is null) return;

        subscription.Status = status;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CancelSubscriptionAsync(string stripeSubscriptionId, CancellationToken cancellationToken)
    {
        var subscription = await dbContext.Subscriptions
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscriptionId, cancellationToken);
        if (subscription is null) return;

        subscription.Status = SubscriptionStatus.Cancelled;
        subscription.CancelledAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

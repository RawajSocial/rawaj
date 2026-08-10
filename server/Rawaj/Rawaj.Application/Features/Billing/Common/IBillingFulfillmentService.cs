using Rawaj.Application.Features.Billing.ChangeSubscriptionPlan;
using Rawaj.Application.Features.Billing.PurchaseAddOn;
using Rawaj.Application.Features.Billing.PurchaseCoins;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Billing.Common;

/// <summary>
/// The single place that actually grants coins/add-ons/subscriptions and records the
/// <c>BillingTransaction</c> for it. Called from two places: the free-plan (Cost == 0) synchronous
/// path, which has no Stripe involvement at all, and the Stripe webhook handlers, once Stripe has
/// confirmed a real charge. Never called directly from a controller for a paid flow — that would
/// grant before payment is confirmed.
/// </summary>
public interface IBillingFulfillmentService
{
    Task<PurchaseCoinsResponse> FulfillCoinPurchaseAsync(
        Guid tenantId, Guid? coinPackageId, int? customCoins,
        string? stripeSessionId, string? stripeEventId, CancellationToken cancellationToken);

    Task<PurchaseAddOnResponse> FulfillAddOnPurchaseAsync(
        Guid tenantId, AddOnType type,
        string? stripeSessionId, string? stripeEventId, CancellationToken cancellationToken);

    Task<ChangeSubscriptionPlanResponse> FulfillPlanChangeAsync(
        Guid tenantId, Guid subscriptionPlanId, string? agencySize, List<string>? servicesOffered,
        string? stripeCustomerId, string? stripeSubscriptionId,
        string? stripeSessionId, string? stripeEventId, CancellationToken cancellationToken);

    /// <summary>A renewal invoice on an existing subscription — re-grants
    /// <c>SubscriptionPlan.MonthlyCoinGrant</c> and advances the current period. Looked up by
    /// <c>Subscription.StripeSubscriptionId</c>, not tenant id, since the webhook only carries the
    /// Stripe subscription/invoice.</summary>
    Task GrantSubscriptionRenewalAsync(string stripeSubscriptionId, string stripeEventId, CancellationToken cancellationToken);

    Task UpdateSubscriptionStatusAsync(string stripeSubscriptionId, SubscriptionStatus status, CancellationToken cancellationToken);

    Task CancelSubscriptionAsync(string stripeSubscriptionId, CancellationToken cancellationToken);

    /// <summary>True if a <c>BillingTransaction</c> already recorded this Stripe event id — the
    /// idempotency check every webhook handler runs before doing any work, since Stripe delivers
    /// events at-least-once.</summary>
    Task<bool> IsEventAlreadyProcessedAsync(string stripeEventId, CancellationToken cancellationToken);

    /// <summary>True if a <c>BillingTransaction</c> already recorded this Checkout Session — used
    /// instead of <see cref="IsEventAlreadyProcessedAsync"/> for checkout.session.completed, since
    /// the same session can be confirmed via two independent paths (the webhook, and the "verify on
    /// return" fallback), each with its own distinct event id but the same underlying session.</summary>
    Task<bool> IsSessionAlreadyProcessedAsync(string stripeSessionId, CancellationToken cancellationToken);
}

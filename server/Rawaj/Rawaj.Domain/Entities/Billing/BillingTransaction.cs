using Rawaj.Domain.Common;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;

namespace Rawaj.Domain.Entities.Billing;

/// <summary>
/// A record of money spent against a tenant — coin package purchases, plan changes, and add-on
/// purchases. Backs the billing page's real invoice/history list.
///
/// Paid transactions (<see cref="StripeEventId"/> set) are created only once Stripe confirms the
/// charge via webhook, never from the browser's return to the success URL. The Free plan
/// (<c>SubscriptionPlan.Cost == 0</c>) is the one case that still writes a row synchronously with
/// no Stripe fields set, since there's nothing to charge.
/// </summary>
public class BillingTransaction : BaseEntity
{
    public Guid TenantId { get; set; }
    public BillingTransactionType Type { get; set; }
    public string Description { get; set; } = null!;
    public decimal AmountUsd { get; set; }
    public int? CoinsGranted { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>The Stripe Checkout Session that produced this transaction, when paid via Stripe.</summary>
    public string? StripeSessionId { get; set; }

    /// <summary>The Stripe webhook event id that fulfilled this transaction — the idempotency key
    /// preventing Stripe's at-least-once delivery from double-granting the same purchase.</summary>
    public string? StripeEventId { get; set; }

    public Tenant Tenant { get; set; } = null!;
}

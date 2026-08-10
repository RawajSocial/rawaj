namespace Rawaj.Application.Common.Models;

/// <summary>Request to start a one-off Stripe Checkout payment (coins or an add-on) — priced via
/// inline <c>price_data</c>, no pre-provisioned Stripe Product/Price needed.</summary>
public record OneOffCheckoutSessionRequest(
    Guid TenantId,
    string ProductName,
    decimal AmountUsd,
    IReadOnlyDictionary<string, string> Metadata);

/// <summary>Request to start a recurring Stripe Checkout subscription (a paid plan change) — priced
/// via inline <c>price_data.recurring</c>. <paramref name="OwnerEmail"/> is used to create the
/// Stripe Customer the first time a tenant subscribes.</summary>
public record SubscriptionCheckoutSessionRequest(
    Guid TenantId,
    string OwnerEmail,
    string? ExistingStripeCustomerId,
    string PlanName,
    decimal AmountUsd,
    bool IsYearly,
    IReadOnlyDictionary<string, string> Metadata);

/// <summary>Result of creating a Checkout Session — the URL to redirect the browser to, the session
/// id itself (for tracking/idempotency bookkeeping), the Stripe Customer id (created or reused) so
/// the caller can persist it onto <c>Subscription</c> before the webhook round-trips, and when
/// Stripe will consider the session expired (used to know how long a still-open session can be
/// safely reused for — see <c>IPendingCheckoutSessionService</c>).</summary>
public record CheckoutSessionResult(string SessionId, string CheckoutUrl, string? StripeCustomerId, DateTime ExpiresAt);

/// <summary>A verified, gateway-agnostic view of a Stripe webhook event — Application code never
/// references Stripe.net types directly, only this DTO. Fields not relevant to a given
/// <see cref="EventType"/> are null.</summary>
public record PaymentWebhookEvent(
    string EventId,
    string EventType,
    string? CheckoutSessionId,
    string? StripeCustomerId,
    string? StripeSubscriptionId,
    /// <summary>Stripe's <c>invoice.billing_reason</c> — distinguishes a subscription's first
    /// invoice (<c>subscription_create</c>, already fulfilled by checkout.session.completed) from a
    /// renewal (<c>subscription_cycle</c>) so renewal handling doesn't double-grant.</summary>
    string? BillingReason,
    /// <summary>Stripe's subscription/invoice <c>status</c> string (e.g. "active", "past_due",
    /// "canceled") for status-changing events.</summary>
    string? Status,
    IReadOnlyDictionary<string, string> Metadata);

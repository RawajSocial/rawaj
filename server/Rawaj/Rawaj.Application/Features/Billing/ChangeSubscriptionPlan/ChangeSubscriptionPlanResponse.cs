using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Billing.ChangeSubscriptionPlan;

/// <summary>
/// For the Free plan (<c>Cost == 0</c>), the change applies synchronously and every field below
/// reflects the new state, with <see cref="CheckoutUrl"/> null. For a paid plan, the change has NOT
/// happened yet — <see cref="CheckoutUrl"/> is set and every other field is a snapshot of the
/// still-current (pre-change) subscription; the caller must redirect the browser there and wait for
/// the Stripe webhook to actually apply the change.
/// </summary>
public record ChangeSubscriptionPlanResponse(
    Guid SubscriptionId,
    string PlanName,
    decimal PlanCost,
    SubscriptionStatus Status,
    DateTime CurrentPeriodStart,
    DateTime CurrentPeriodEnd,
    TenantType TenantType,
    int NewCoinBalance,
    int CoinsGranted,
    string? CheckoutUrl = null);

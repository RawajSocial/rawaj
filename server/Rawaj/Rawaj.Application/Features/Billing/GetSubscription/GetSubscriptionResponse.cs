using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Billing.GetSubscription;

public record GetSubscriptionResponse(
    Guid SubscriptionId,
    string PlanName,
    decimal PlanCost,
    SubscriptionStatus Status,
    BillingCycle BillingCycle,
    DateTime CurrentPeriodStart,
    DateTime CurrentPeriodEnd,
    DateTime? TrialEndsAt);

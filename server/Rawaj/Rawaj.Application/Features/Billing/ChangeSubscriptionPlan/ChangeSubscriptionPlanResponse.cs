using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Billing.ChangeSubscriptionPlan;

public record ChangeSubscriptionPlanResponse(
    Guid SubscriptionId,
    string PlanName,
    decimal PlanCost,
    SubscriptionStatus Status,
    DateTime CurrentPeriodStart,
    DateTime CurrentPeriodEnd);

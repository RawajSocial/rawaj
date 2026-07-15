using Rawaj.Domain.Common;
using Rawaj.Domain.Enums;

namespace Rawaj.Domain.Entities.Billing;

public class Subscription : BaseEntity
{
    public Guid SubscriptionPlanId { get; set; }
    public BillingCycle BillingCycle { get; set; }
    public SubscriptionStatus Status { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public string? StripeCustomerId { get; set; }
    public DateTime CurrentPeriodStart { get; set; }
    public DateTime CurrentPeriodEnd { get; set; }
    public DateTime? TrialEndsAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public SubscriptionPlan SubscriptionPlan { get; set; } = null!;
}

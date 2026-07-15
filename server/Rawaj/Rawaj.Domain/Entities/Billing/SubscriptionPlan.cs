using Rawaj.Domain.Common;
using Rawaj.Domain.Enums;

namespace Rawaj.Domain.Entities.Billing;

public class SubscriptionPlan : BaseEntity
{
    public string Name { get; set; } = null!;
    public decimal Cost { get; set; }
    public BillingCycle BillingCycle { get; set; }
    public string Currency { get; set; } = null!;
    public int MaxBrands { get; set; }
    public int MaxUsers { get; set; }
    public int MaxCampaignsMonthly { get; set; }
    public int MaxAiCreditsMonthly { get; set; }
    public int MaxScheduledPosts { get; set; }
    public int MaxSocialAccounts { get; set; }
    public List<string> Features { get; set; } = [];
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<Subscription> Subscriptions { get; set; } = [];
}

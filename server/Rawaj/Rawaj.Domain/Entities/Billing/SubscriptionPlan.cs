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
    /// <summary>Percentage discount (0-100) applied to every coin cost for tenants on this plan
    /// (see <see cref="Rawaj.Application.Common.Policies.CoinPricingPolicy"/>).</summary>
    public int CoinUsageDiscountPercent { get; set; }
    /// <summary>Coins credited to the tenant's pool whenever they subscribe to this plan (see
    /// <c>ChangeSubscriptionPlanCommandHandler</c>) — at most once per billing period, tracked via
    /// <see cref="Subscription.LastCoinGrantAt"/>.</summary>
    public int MonthlyCoinGrant { get; set; }
    public List<string> Features { get; set; } = [];
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<Subscription> Subscriptions { get; set; } = [];
}

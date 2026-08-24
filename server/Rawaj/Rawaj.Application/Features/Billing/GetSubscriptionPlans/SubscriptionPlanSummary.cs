using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Billing.GetSubscriptionPlans;

public record SubscriptionPlanSummary(
    Guid SubscriptionPlanId,
    string Name,
    decimal Cost,
    BillingCycle BillingCycle,
    string Currency,
    int MaxBrands,
    int MaxUsers,
    int MaxCampaignsMonthly,
    int MaxAiCreditsMonthly,
    int MaxScheduledPosts,
    int MaxSocialAccounts,
    int CoinUsageDiscountPercent,
    int MonthlyCoinGrant,
    List<string> Features);

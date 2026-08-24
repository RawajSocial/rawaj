namespace Rawaj.Domain.Enums;

public enum BillingCycle
{
    Monthly,
    Yearly
}

public enum SubscriptionStatus
{
    Active,
    Cancelled,
    PastDue,
    Trialing
}

/// <summary>What a <see cref="Rawaj.Domain.Entities.Billing.BillingTransaction"/> row represents.</summary>
public enum BillingTransactionType
{
    CoinPurchase,
    PlanChange,
    AddOnPurchase,
    CoinGrant
}

/// <summary>The two paid add-ons a tenant can buy to raise their plan's brand/marketeer caps.</summary>
public enum AddOnType
{
    ExtraBrand,
    ExtraMarketeer
}

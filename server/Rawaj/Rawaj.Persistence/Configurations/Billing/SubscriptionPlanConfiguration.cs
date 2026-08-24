using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rawaj.Domain.Entities.Billing;
using Rawaj.Domain.Enums;
using Rawaj.Persistence.Common;

namespace Rawaj.Persistence.Configurations.Billing;

public class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    public static readonly Guid FreePlanId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public static readonly Guid ProPlanId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    public static readonly Guid PlusPlanId = Guid.Parse("00000000-0000-0000-0000-000000000003");
    public static readonly Guid UltraPlanId = Guid.Parse("00000000-0000-0000-0000-000000000004");

    private static readonly DateTime SeedCreatedAt = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        builder.ToTable("subscription_plans");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(p => p.Name).IsUnique();

        builder.Property(p => p.Cost).HasColumnType("decimal(10,2)").IsRequired();
        builder.Property(p => p.BillingCycle).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(p => p.Currency).HasMaxLength(5).IsRequired();
        builder.Property(p => p.Features).HasJsonConversion().HasColumnType("nvarchar(max)").IsRequired();

        // Free/Plus/Pro/Ultra: MaxUsers = the pricing sheet's "Marketeers" + 1 (the owner already
        // occupies a seat). Free is bumped to 2 (owner + 1 invite) and given 1 campaign/month so a
        // brand-new account can actually exercise the invite and campaign flows without upgrading —
        // previously MaxUsers=1/MaxCampaignsMonthly=0 made both permanently unreachable on Free.
        builder.HasData(new SubscriptionPlan
        {
            Id = FreePlanId,
            Name = "Free",
            Cost = 0,
            BillingCycle = BillingCycle.Monthly,
            Currency = "USD",
            MaxBrands = 1,
            MaxUsers = 2,
            MaxCampaignsMonthly = 1,
            MaxAiCreditsMonthly = 50,
            MaxScheduledPosts = 10,
            MaxSocialAccounts = 2,
            CoinUsageDiscountPercent = 0,
            MonthlyCoinGrant = 100,
            Features = [],
            IsActive = true,
            CreatedAt = SeedCreatedAt
        });

        builder.HasData(new SubscriptionPlan
        {
            Id = PlusPlanId,
            Name = "Plus",
            Cost = 99,
            BillingCycle = BillingCycle.Monthly,
            Currency = "USD",
            MaxBrands = 10,
            MaxUsers = 9,
            MaxCampaignsMonthly = 50,
            MaxAiCreditsMonthly = 1000,
            MaxScheduledPosts = 200,
            MaxSocialAccounts = 10,
            CoinUsageDiscountPercent = 10,
            MonthlyCoinGrant = 10000,
            Features = ["priority_support"],
            IsActive = true,
            CreatedAt = SeedCreatedAt
        });

        builder.HasData(new SubscriptionPlan
        {
            Id = ProPlanId,
            Name = "Pro",
            Cost = 249,
            BillingCycle = BillingCycle.Monthly,
            Currency = "USD",
            MaxBrands = 30,
            MaxUsers = 26,
            MaxCampaignsMonthly = 150,
            MaxAiCreditsMonthly = 5000,
            MaxScheduledPosts = 1000,
            MaxSocialAccounts = 30,
            CoinUsageDiscountPercent = 18,
            MonthlyCoinGrant = 30000,
            Features = ["priority_support", "advanced_analytics"],
            IsActive = true,
            CreatedAt = SeedCreatedAt
        });

        builder.HasData(new SubscriptionPlan
        {
            Id = UltraPlanId,
            Name = "Ultra",
            Cost = 599,
            BillingCycle = BillingCycle.Monthly,
            Currency = "USD",
            MaxBrands = 100,
            MaxUsers = 101,
            MaxCampaignsMonthly = 500,
            MaxAiCreditsMonthly = 20000,
            MaxScheduledPosts = 5000,
            MaxSocialAccounts = 100,
            CoinUsageDiscountPercent = 30,
            MonthlyCoinGrant = 80000,
            Features = ["priority_support", "advanced_analytics", "dedicated_account_manager"],
            IsActive = true,
            CreatedAt = SeedCreatedAt
        });
    }
}

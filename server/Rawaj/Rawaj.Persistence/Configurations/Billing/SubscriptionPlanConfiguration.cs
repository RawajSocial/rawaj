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

        builder.HasData(new SubscriptionPlan
        {
            Id = FreePlanId,
            Name = "Free",
            Cost = 0,
            BillingCycle = BillingCycle.Monthly,
            Currency = "USD",
            MaxBrands = 1,
            MaxUsers = 3,
            MaxCampaignsMonthly = 0,
            MaxAiCreditsMonthly = 50,
            MaxScheduledPosts = 10,
            MaxSocialAccounts = 2,
            Features = [],
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        builder.HasData(new SubscriptionPlan
        {
            Id = ProPlanId,
            Name = "Pro",
            Cost = 49.99m,
            BillingCycle = BillingCycle.Monthly,
            Currency = "USD",
            MaxBrands = 5,
            MaxUsers = 10,
            MaxCampaignsMonthly = 50,
            MaxAiCreditsMonthly = 1000,
            MaxScheduledPosts = 200,
            MaxSocialAccounts = 10,
            Features = ["priority_support", "advanced_analytics"],
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
    }
}

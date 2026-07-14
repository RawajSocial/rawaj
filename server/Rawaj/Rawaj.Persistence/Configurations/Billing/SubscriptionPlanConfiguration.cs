using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rawaj.Domain.Entities.Billing;
using Rawaj.Domain.Enums;
using Rawaj.Persistence.Common;

namespace Rawaj.Persistence.Configurations.Billing;

public class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
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
            Id = WellKnownIds.FreeSubscriptionPlanId,
            Name = "Free",
            Cost = 0,
            BillingCycle = BillingCycle.Monthly,
            Currency = "USD",
            MaxBrands = 1,
            MaxUsers = 1,
            MaxCampaignsMonthly = 5,
            MaxAiCreditsMonthly = 50,
            MaxScheduledPosts = 10,
            MaxSocialAccounts = 1,
            Features = [],
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
    }
}

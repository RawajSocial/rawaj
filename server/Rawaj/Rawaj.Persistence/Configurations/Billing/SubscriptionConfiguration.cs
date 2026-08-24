using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rawaj.Domain.Entities.Billing;

namespace Rawaj.Persistence.Configurations.Billing;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("subscriptions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.BillingCycle).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(s => s.StripeSubscriptionId).HasMaxLength(255);
        builder.Property(s => s.StripeCustomerId).HasMaxLength(255);

        builder.HasOne(s => s.SubscriptionPlan)
            .WithMany(p => p.Subscriptions)
            .HasForeignKey(s => s.SubscriptionPlanId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

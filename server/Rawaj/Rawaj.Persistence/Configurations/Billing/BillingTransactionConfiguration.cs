using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rawaj.Domain.Entities.Billing;
using Rawaj.Domain.Entities.Tenants;

namespace Rawaj.Persistence.Configurations.Billing;

public class BillingTransactionConfiguration : IEntityTypeConfiguration<BillingTransaction>
{
    public void Configure(EntityTypeBuilder<BillingTransaction> builder)
    {
        builder.ToTable("billing_transactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(500).IsRequired();
        builder.Property(t => t.AmountUsd).HasColumnType("decimal(10,2)").IsRequired();
        builder.Property(t => t.StripeSessionId).HasMaxLength(200);
        builder.Property(t => t.StripeEventId).HasMaxLength(200);

        builder.HasIndex(t => new { t.TenantId, t.CreatedAt });

        // Idempotency guard against Stripe's at-least-once webhook delivery: a duplicate delivery of
        // an already-processed event hits this unique constraint instead of double-granting.
        builder.HasIndex(t => t.StripeEventId).IsUnique().HasFilter("[StripeEventId] IS NOT NULL");

        // A second, independent idempotency guard on the session itself: the same Checkout Session
        // can be confirmed via two racing paths (the real webhook and the "verify on return"
        // fallback), each carrying a different Stripe event id — this is what actually stops a race
        // between them from double-granting, since the application-level pre-check alone isn't atomic.
        builder.HasIndex(t => t.StripeSessionId).IsUnique().HasFilter("[StripeSessionId] IS NOT NULL");

        builder.HasOne(t => t.Tenant)
            .WithMany()
            .HasForeignKey(t => t.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

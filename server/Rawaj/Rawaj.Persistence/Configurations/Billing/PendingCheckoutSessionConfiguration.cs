using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rawaj.Domain.Entities.Billing;
using Rawaj.Domain.Entities.Tenants;

namespace Rawaj.Persistence.Configurations.Billing;

public class PendingCheckoutSessionConfiguration : IEntityTypeConfiguration<PendingCheckoutSession>
{
    public void Configure(EntityTypeBuilder<PendingCheckoutSession> builder)
    {
        builder.ToTable("pending_checkout_sessions");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.IntentKey).HasMaxLength(200).IsRequired();
        builder.Property(p => p.StripeSessionId).HasMaxLength(200).IsRequired();
        builder.Property(p => p.CheckoutUrl).HasMaxLength(1000).IsRequired();

        // At most one open session per tenant per purchase intent — this is the actual guard
        // against a race between two near-simultaneous requests both finding "no open session yet".
        builder.HasIndex(p => new { p.TenantId, p.IntentKey }).IsUnique();

        builder.HasOne(p => p.Tenant)
            .WithMany()
            .HasForeignKey(p => p.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

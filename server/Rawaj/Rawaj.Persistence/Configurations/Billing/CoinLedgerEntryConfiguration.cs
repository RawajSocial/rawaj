using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rawaj.Domain.Entities.Billing;
using Rawaj.Domain.Entities.Tenants;

namespace Rawaj.Persistence.Configurations.Billing;

public class CoinLedgerEntryConfiguration : IEntityTypeConfiguration<CoinLedgerEntry>
{
    public void Configure(EntityTypeBuilder<CoinLedgerEntry> builder)
    {
        builder.ToTable("coin_ledger_entries");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Reason).HasMaxLength(100).IsRequired();
        builder.Property(e => e.ReferenceType).HasMaxLength(100);

        builder.HasIndex(e => new { e.TenantId, e.CreatedAt });

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<TenantMember>()
            .WithMany()
            .HasForeignKey(e => e.TenantMemberId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

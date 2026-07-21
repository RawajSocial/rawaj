using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Persistence.Identity;

namespace Rawaj.Persistence.Configurations.Tenants;

public class TenantMemberConfiguration : IEntityTypeConfiguration<TenantMember>
{
    public void Configure(EntityTypeBuilder<TenantMember> builder)
    {
        builder.ToTable("tenant_members");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Role).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(m => m.InvitationStatus).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(m => m.InvitedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => new { m.TenantId, m.UserId }).IsUnique();
    }
}

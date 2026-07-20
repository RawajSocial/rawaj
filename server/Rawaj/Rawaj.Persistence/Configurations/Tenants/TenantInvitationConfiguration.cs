using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Persistence.Identity;

namespace Rawaj.Persistence.Configurations.Tenants;

public class TenantInvitationConfiguration : IEntityTypeConfiguration<TenantInvitation>
{
    public void Configure(EntityTypeBuilder<TenantInvitation> builder)
    {
        builder.ToTable("tenant_invitations");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Email).HasMaxLength(255).IsRequired();
        builder.HasIndex(i => i.Email);

        builder.Property(i => i.Role).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(i => i.ExpiresAt).IsRequired();

        builder.HasOne(i => i.Tenant)
            .WithMany()
            .HasForeignKey(i => i.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(i => i.InvitedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => new { i.TenantId, i.Email })
            .IsUnique()
            .HasFilter("[Status] = 'Pending'");
    }
}

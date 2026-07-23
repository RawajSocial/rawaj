using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rawaj.Domain.Entities.Platform;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Persistence.Identity;

namespace Rawaj.Persistence.Configurations.Platform;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("logs");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Action).HasMaxLength(100).IsRequired();
        builder.Property(l => l.Message).HasColumnType("nvarchar(max)");
        builder.Property(l => l.EntityType).HasMaxLength(100);
        builder.Property(l => l.IpAddress).HasMaxLength(45);
        builder.Property(l => l.UserAgent).HasColumnType("nvarchar(max)");
        builder.Property(l => l.Metadata).HasColumnType("nvarchar(max)");

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(l => l.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => l.TenantId);
    }
}

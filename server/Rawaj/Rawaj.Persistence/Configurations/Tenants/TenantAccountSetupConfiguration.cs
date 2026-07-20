using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Persistence.Common;

namespace Rawaj.Persistence.Configurations.Tenants;

public class TenantAccountSetupConfiguration : IEntityTypeConfiguration<TenantAccountSetup>
{
    public void Configure(EntityTypeBuilder<TenantAccountSetup> builder)
    {
        builder.ToTable("tenant_account_setups");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.AccountType).HasMaxLength(20).IsRequired();
        builder.Property(a => a.Country).HasMaxLength(100).IsRequired();
        builder.Property(a => a.City).HasMaxLength(100);

        builder.Property(a => a.SocialLinks!).HasJsonConversion().HasColumnType("nvarchar(max)");
        builder.Property(a => a.AgencyDetails!).HasJsonConversion().HasColumnType("nvarchar(max)");
        builder.Property(a => a.BusinessDetails!).HasJsonConversion().HasColumnType("nvarchar(max)");

        builder.HasIndex(a => a.BrandProfileId).IsUnique();

        builder.HasOne(a => a.BrandProfile)
            .WithMany()
            .HasForeignKey(a => a.BrandProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Persistence.Common;

namespace Rawaj.Persistence.Configurations.Tenants;

public class TenantBrandProfileConfiguration : IEntityTypeConfiguration<TenantBrandProfile>
{
    public void Configure(EntityTypeBuilder<TenantBrandProfile> builder)
    {
        builder.ToTable("tenant_brand_profiles");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Name).HasMaxLength(200).IsRequired();
        builder.Property(b => b.Description).HasColumnType("nvarchar(max)");
        builder.Property(b => b.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(b => b.BrandInfo!).HasJsonConversion().HasColumnType("nvarchar(max)");
        builder.Property(b => b.RowVersion).IsRowVersion();

        // ai_artifacts references tenant_brand_profiles, so this closes a cycle — NoAction, same as
        // the campaign-side pointers.
        builder.HasOne(b => b.CurrentBrandAnalysisArtifact)
            .WithMany()
            .HasForeignKey(b => b.CurrentBrandAnalysisArtifactId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(b => b.TenantId);

        builder.HasQueryFilter(b => !b.IsDeleted);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.Entities.Tenants;

namespace Rawaj.Persistence.Configurations.Campaigns;

public class VisualAssetConfiguration : IEntityTypeConfiguration<VisualAsset>
{
    public void Configure(EntityTypeBuilder<VisualAsset> builder)
    {
        builder.ToTable("visual_assets");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(v => v.FileUrl).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(v => v.SourceType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(v => v.GenerationMode).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(v => v.AiPrompt).HasColumnType("nvarchar(max)");
        builder.Property(v => v.AiModel).HasMaxLength(100);
        builder.Property(v => v.Format).HasMaxLength(20);
        builder.Property(v => v.PublicId).HasMaxLength(300);
        builder.Property(v => v.MimeType).HasMaxLength(100);
        builder.Property(v => v.StorageProvider).HasMaxLength(50);

        builder.HasOne(v => v.ContentItem)
            .WithMany(c => c.VisualAssets)
            .HasForeignKey(v => v.ContentItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.Campaign)
            .WithMany(c => c.VisualAssets)
            .HasForeignKey(v => v.CampaignId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.BrandProfile)
            .WithMany(b => b.VisualAssets)
            .HasForeignKey(v => v.BrandProfileId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.VersionOfAsset)
            .WithMany()
            .HasForeignKey(v => v.VersionOf)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(v => v.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(v => !v.IsDeleted);
    }
}

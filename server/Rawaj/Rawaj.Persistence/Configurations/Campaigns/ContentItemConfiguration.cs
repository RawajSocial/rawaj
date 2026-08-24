using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Persistence.Common;
using Rawaj.Persistence.Identity;

namespace Rawaj.Persistence.Configurations.Campaigns;

public class ContentItemConfiguration : IEntityTypeConfiguration<ContentItem>
{
    public void Configure(EntityTypeBuilder<ContentItem> builder)
    {
        builder.ToTable("content_items");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.ContentType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(c => c.Platform).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(c => c.Language).HasConversion<string>().HasMaxLength(5).IsRequired();
        builder.Property(c => c.Title).HasMaxLength(255);
        builder.Property(c => c.Content).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(c => c.Hashtags).HasJsonConversion().HasColumnType("nvarchar(max)");
        builder.Property(c => c.Cta).HasMaxLength(255);
        builder.Property(c => c.Tone).HasMaxLength(50);
        builder.Property(c => c.ImagePrompt).HasColumnType("nvarchar(max)");
        builder.Property(c => c.SuggestedPostAt).HasUtcConversion();
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(c => c.GenerationMode).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(c => c.RowVersion).IsRowVersion();

        builder.HasOne(c => c.Campaign)
            .WithMany(m => m.ContentItems)
            .HasForeignKey(c => c.CampaignId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(c => c.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.BrandProfile)
            .WithMany(b => b.ContentItems)
            .HasForeignKey(c => c.BrandProfileId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(c => c.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(c => c.ReviewedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // No cycle here, so this one can carry real behaviour: purging pipeline history detaches the
        // provenance pointer and leaves the post itself alone. The post is the deliverable; which
        // stage produced it is metadata.
        builder.HasOne(c => c.PipelineStage)
            .WithMany()
            .HasForeignKey(c => c.PipelineStageId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(c => new { c.BrandProfileId, c.Status });
        builder.HasIndex(c => new { c.TenantId, c.Status });
        builder.HasIndex(c => c.PipelineStageId);

        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}

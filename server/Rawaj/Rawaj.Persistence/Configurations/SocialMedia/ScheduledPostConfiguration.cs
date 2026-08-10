using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rawaj.Domain.Entities.SocialMedia;
using Rawaj.Persistence.Common;

namespace Rawaj.Persistence.Configurations.SocialMedia;

public class ScheduledPostConfiguration : IEntityTypeConfiguration<ScheduledPost>
{
    public void Configure(EntityTypeBuilder<ScheduledPost> builder)
    {
        builder.ToTable("scheduled_posts");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(s => s.PostId).HasMaxLength(255);
        builder.Property(s => s.ErrorMessage).HasColumnType("nvarchar(max)");
        builder.Property(s => s.ScheduledAt).HasUtcConversion();
        builder.Property(s => s.PublishedAt).HasUtcConversion();

        builder.HasOne(s => s.ContentItem)
            .WithMany()
            .HasForeignKey(s => s.ContentItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.VisualAsset)
            .WithMany()
            .HasForeignKey(s => s.VisualAssetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.SocialAccount)
            .WithMany(a => a.ScheduledPosts)
            .HasForeignKey(s => s.SocialAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.BrandProfile)
            .WithMany()
            .HasForeignKey(s => s.BrandProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Campaign)
            .WithMany()
            .HasForeignKey(s => s.CampaignId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => new { s.Status, s.ScheduledAt });
        builder.HasIndex(s => new { s.BrandProfileId, s.ScheduledAt });

        // Matches TenantBrandProfile's own IsDeleted filter — required (non-nullable) FK — plus this
        // row's own IsDeleted, set when a campaign delete cascades to its scheduled posts.
        builder.HasQueryFilter(s => !s.BrandProfile.IsDeleted && !s.IsDeleted);
    }
}

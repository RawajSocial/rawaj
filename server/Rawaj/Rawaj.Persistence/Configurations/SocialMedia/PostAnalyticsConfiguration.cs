using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rawaj.Domain.Entities.SocialMedia;

namespace Rawaj.Persistence.Configurations.SocialMedia;

public class PostAnalyticsConfiguration : IEntityTypeConfiguration<PostAnalytics>
{
    public void Configure(EntityTypeBuilder<PostAnalytics> builder)
    {
        builder.ToTable("post_analytics");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Platform).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(p => p.EngagementRate).HasColumnType("decimal(5,4)");
        builder.Property(p => p.RawData).HasColumnType("nvarchar(max)");

        builder.HasOne(p => p.ScheduledPost)
            .WithMany(s => s.Analytics)
            .HasForeignKey(p => p.ScheduledPostId)
            .OnDelete(DeleteBehavior.Restrict);

        // Matches ScheduledPost's own filter (itself cascaded from TenantBrandProfile.IsDeleted)
        // — required (non-nullable) FK.
        builder.HasQueryFilter(p => !p.ScheduledPost.BrandProfile.IsDeleted);
    }
}

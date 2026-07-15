using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rawaj.Domain.Entities.SocialMedia;

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
    }
}

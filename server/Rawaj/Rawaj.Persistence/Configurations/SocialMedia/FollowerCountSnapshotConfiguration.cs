using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rawaj.Domain.Entities.SocialMedia;

namespace Rawaj.Persistence.Configurations.SocialMedia;

public class FollowerCountSnapshotConfiguration : IEntityTypeConfiguration<FollowerCountSnapshot>
{
    public void Configure(EntityTypeBuilder<FollowerCountSnapshot> builder)
    {
        builder.ToTable("follower_count_snapshots");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Platform).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasOne(s => s.SocialAccount)
            .WithMany()
            .HasForeignKey(s => s.SocialAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        // Supports "most recent snapshot at or before a cutoff" lookups (month-over-month follower
        // comparisons) — descending on RecordedAt so the nearest-before-cutoff row per account is
        // found without a full scan, mirroring PostAnalyticsConfiguration's own index.
        builder.HasIndex(s => new { s.SocialAccountId, s.RecordedAt }).IsDescending(false, true);

        builder.HasQueryFilter(s => !s.SocialAccount.BrandProfile.IsDeleted);
    }
}

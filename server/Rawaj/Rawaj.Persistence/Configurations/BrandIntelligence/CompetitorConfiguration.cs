using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rawaj.Domain.Entities.BrandIntelligence;
using Rawaj.Persistence.Common;

namespace Rawaj.Persistence.Configurations.BrandIntelligence;

public class CompetitorConfiguration : IEntityTypeConfiguration<Competitor>
{
    public void Configure(EntityTypeBuilder<Competitor> builder)
    {
        builder.ToTable("competitors");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.SocialHandles).HasJsonConversion().HasColumnType("nvarchar(max)");
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasOne(c => c.BrandProfile)
            .WithMany()
            .HasForeignKey(c => c.BrandProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.RagDocuments)
            .WithOne(r => r.Competitor)
            .HasForeignKey(r => r.CompetitorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

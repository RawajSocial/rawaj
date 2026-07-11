using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rawaj.Domain.Entities.BrandIntelligence;

namespace Rawaj.Persistence.Configurations.BrandIntelligence;

public class RagDocumentConfiguration : IEntityTypeConfiguration<RagDocument>
{
    public void Configure(EntityTypeBuilder<RagDocument> builder)
    {
        builder.ToTable("rag_documents");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.SourceType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(r => r.VectorId).HasMaxLength(255);
        builder.Property(r => r.CompetitorsData).HasColumnType("nvarchar(max)");

        builder.HasOne(r => r.BrandProfile)
            .WithMany()
            .HasForeignKey(r => r.BrandProfileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

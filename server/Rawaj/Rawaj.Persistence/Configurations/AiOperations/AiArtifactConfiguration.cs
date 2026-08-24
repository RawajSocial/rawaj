using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rawaj.Domain.Entities.AiOperations;

namespace Rawaj.Persistence.Configurations.AiOperations;

public class AiArtifactConfiguration : IEntityTypeConfiguration<AiArtifact>
{
    public void Configure(EntityTypeBuilder<AiArtifact> builder)
    {
        builder.ToTable("ai_artifacts");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Kind).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(a => a.ContentJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(a => a.InputHash).HasMaxLength(64);

        builder.HasOne(a => a.Tenant)
            .WithMany()
            .HasForeignKey(a => a.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.BrandProfile)
            .WithMany()
            .HasForeignKey(a => a.BrandProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Campaign)
            .WithMany()
            .HasForeignKey(a => a.CampaignId)
            .OnDelete(DeleteBehavior.Restrict);

        // See AiPipelineStageConfiguration for why this side of the cycle is also NoAction.
        builder.HasOne(a => a.SourceStage)
            .WithMany()
            .HasForeignKey(a => a.SourceStageId)
            .OnDelete(DeleteBehavior.NoAction);

        // "Give me the current strategy for this campaign" — the shape of nearly every artifact read.
        builder.HasIndex(a => new { a.CampaignId, a.Kind, a.IsCurrent });

        // The brand-analysis cache lookup: is there already an artifact for this brand whose inputs
        // hash to what BrandInfo hashes to right now?
        builder.HasIndex(a => new { a.BrandProfileId, a.Kind, a.InputHash });

        // Version numbering is per scope+kind, and the store increments it by reading the current
        // maximum — this is the index that read uses.
        builder.HasIndex(a => new { a.CampaignId, a.Kind, a.Version });
    }
}

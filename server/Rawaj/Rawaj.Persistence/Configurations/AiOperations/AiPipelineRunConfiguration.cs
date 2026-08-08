using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rawaj.Domain.Entities.AiOperations;
using Rawaj.Persistence.Identity;

namespace Rawaj.Persistence.Configurations.AiOperations;

public class AiPipelineRunConfiguration : IEntityTypeConfiguration<AiPipelineRun>
{
    public void Configure(EntityTypeBuilder<AiPipelineRun> builder)
    {
        builder.ToTable("ai_pipeline_runs");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(r => r.CurrentStage).HasConversion<string>().HasMaxLength(40);
        builder.Property(r => r.LastError).HasColumnType("nvarchar(max)");
        builder.Property(r => r.ContentLanguage).HasConversion<string>().HasMaxLength(5).IsRequired();
        builder.Property(r => r.ContentTemplateStyle).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(r => r.RowVersion).IsRowVersion();

        builder.HasOne(r => r.Tenant)
            .WithMany()
            .HasForeignKey(r => r.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.BrandProfile)
            .WithMany()
            .HasForeignKey(r => r.BrandProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Campaign)
            .WithMany()
            .HasForeignKey(r => r.CampaignId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(r => r.TriggeredBy)
            .OnDelete(DeleteBehavior.Restrict);

        // A run's stages are meaningless without it — unlike every other relationship here, which is
        // Restrict because the referenced rows have a life of their own.
        builder.HasMany(r => r.Stages)
            .WithOne(s => s.Run)
            .HasForeignKey(s => s.RunId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => new { r.TenantId, r.Status });
        builder.HasIndex(r => new { r.CampaignId, r.CreatedAt });

        // The worker asks "is there anything to do at all" on every tick. Without a filter this
        // index spans every run the system has ever executed; with one it stays proportional to
        // what is actually in flight, which is normally a handful of rows.
        builder.HasIndex(r => r.Status)
            .HasDatabaseName("IX_ai_pipeline_runs_Status_Active")
            .HasFilter("[Status] IN ('Pending', 'Running')");
    }
}

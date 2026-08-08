using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rawaj.Domain.Entities.AiOperations;

namespace Rawaj.Persistence.Configurations.AiOperations;

public class AiPipelineStageConfiguration : IEntityTypeConfiguration<AiPipelineStage>
{
    public void Configure(EntityTypeBuilder<AiPipelineStage> builder)
    {
        builder.ToTable("ai_pipeline_stages");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Kind).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(s => s.LastErrorKind).HasConversion<string>().HasMaxLength(20);
        builder.Property(s => s.LastError).HasColumnType("nvarchar(max)");
        builder.Property(s => s.LeaseOwner).HasMaxLength(64);

        // SHA-256 rendered as hex.
        builder.Property(s => s.InputHash).HasMaxLength(64);

        // Matches AiJob.OutputRefType's width — same convention, same kind of value.
        builder.Property(s => s.TargetRefType).HasMaxLength(100);

        // Deliberately NoAction on both ends of the stage↔artifact cycle: a stage points at what it
        // produced, and an artifact records what produced it. Either FK cascading would give SQL
        // Server multiple cascade paths (run → stage → artifact vs. campaign → artifact) and the
        // migration would simply refuse to build. Neither row should disappear implicitly anyway:
        // an artifact outlives re-runs of its stage, and history is the point of keeping both.
        builder.HasOne(s => s.Artifact)
            .WithMany()
            .HasForeignKey(s => s.ArtifactId)
            .OnDelete(DeleteBehavior.NoAction);

        // The stage→AiJob side is a collection, configured from the AiJob end (see
        // AiJobConfiguration): a stage makes one call, several, or none at all.

        // One row per stage of a run — except the fan-out stages, which are distinguished by what
        // they act on (ContentImage: one row per ContentItem). SQL Server treats NULLs as equal in a
        // unique index, which is exactly right here: it forbids a second ContentPlan row for the
        // same run while allowing many ContentImage rows with distinct targets. Retrying a stage
        // reuses its row rather than inserting another, so this also guards against a bug producing
        // duplicate work that would be charged for twice.
        //
        // HasFilter(null) is load-bearing, not decoration: EF's SQL Server provider defaults a
        // unique index over a nullable column to "WHERE [TargetRefId] IS NOT NULL", which would
        // exempt every single-instance stage from the constraint and leave exactly the duplicates
        // this index exists to prevent.
        builder.HasIndex(s => new { s.RunId, s.Kind, s.TargetRefId })
            .IsUnique()
            .HasFilter(null);

        // The worker's claim query. Filtered to Pending because a run that has been executing for
        // weeks still has all its Completed stages sitting in the table, and they are never
        // candidates.
        builder.HasIndex(s => new { s.Status, s.NextAttemptAt })
            .HasDatabaseName("IX_ai_pipeline_stages_Pending")
            .HasFilter("[Status] = 'Pending'");

        // The reaper's query: which claims have expired and need returning to Pending.
        builder.HasIndex(s => new { s.Status, s.LeaseExpiresAt })
            .HasDatabaseName("IX_ai_pipeline_stages_Running_Lease")
            .HasFilter("[Status] = 'Running'");
    }
}

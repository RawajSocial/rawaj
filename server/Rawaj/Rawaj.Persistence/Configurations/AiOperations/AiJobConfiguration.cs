using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rawaj.Domain.Entities.AiOperations;
using Rawaj.Persistence.Identity;

namespace Rawaj.Persistence.Configurations.AiOperations;

public class AiJobConfiguration : IEntityTypeConfiguration<AiJob>
{
    public void Configure(EntityTypeBuilder<AiJob> builder)
    {
        builder.ToTable("ai_jobs");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.JobType).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(a => a.InputParams).HasColumnType("nvarchar(max)");
        builder.Property(a => a.OutputRefType).HasMaxLength(100);
        builder.Property(a => a.Cost).HasColumnType("decimal(10,6)");
        builder.Property(a => a.ErrorMessage).HasColumnType("nvarchar(max)");
        builder.Property(a => a.Provider).HasMaxLength(32);
        builder.Property(a => a.Model).HasMaxLength(64);
        builder.Property(a => a.PromptHash).HasMaxLength(64);

        builder.HasOne(a => a.BrandProfile)
            .WithMany()
            .HasForeignKey(a => a.BrandProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(a => a.TriggeredBy)
            .OnDelete(DeleteBehavior.Restrict);

        // SetNull rather than Restrict: purging old pipeline history must not be blocked by, or
        // destroy, the provider-call log. The call genuinely happened and still costs what it cost —
        // it just stops being attributable to a stage that no longer exists.
        builder.HasOne(a => a.PipelineStage)
            .WithMany(s => s.AiJobs)
            .HasForeignKey(a => a.PipelineStageId)
            .OnDelete(DeleteBehavior.SetNull);

        // TenantId carries no FK on purpose (see the entity) — but it is the column every usage and
        // cost query filters on, so it still needs an index. Paired with CreatedAt because those
        // queries are always "for this tenant, this month".
        builder.HasIndex(a => new { a.TenantId, a.CreatedAt });

        builder.HasIndex(a => a.PipelineStageId);
    }
}

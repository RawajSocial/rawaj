using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Persistence.Common;
using Rawaj.Persistence.Identity;

namespace Rawaj.Persistence.Configurations.Campaigns;

public class MarketingCampaignConfiguration : IEntityTypeConfiguration<MarketingCampaign>
{
    public void Configure(EntityTypeBuilder<MarketingCampaign> builder)
    {
        builder.ToTable("marketing_campaigns");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).HasMaxLength(255).IsRequired();
        builder.Property(c => c.TargetPlatforms).HasJsonConversion().HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(c => c.BudgetAmount).HasColumnType("decimal(12,2)");
        builder.Property(c => c.BudgetCurrency).HasMaxLength(5);
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(c => c.AiPlanJson).HasColumnType("nvarchar(max)");
        builder.Property(c => c.BriefJson).HasColumnType("nvarchar(max)");
        builder.Property(c => c.CompetitorResearchJson).HasColumnType("nvarchar(max)");
        builder.Property(c => c.DiagnosisJson).HasColumnType("nvarchar(max)");
        builder.Property(c => c.RowVersion).IsRowVersion();

        builder.HasOne(c => c.BrandProfile)
            .WithMany(b => b.Campaigns)
            .HasForeignKey(c => c.BrandProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(c => c.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // Both of these point into tables that point back at marketing_campaigns, so both must be
        // NoAction — any delete behaviour here would be a second cascade path into the same table
        // and SQL Server refuses the schema outright. Nothing is lost: a run and an approved
        // strategy are history, and history should not evaporate because a campaign row went away.
        builder.HasOne(c => c.CurrentPipelineRun)
            .WithMany()
            .HasForeignKey(c => c.CurrentPipelineRunId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(c => c.ApprovedStrategyArtifact)
            .WithMany()
            .HasForeignKey(c => c.ApprovedStrategyArtifactId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(c => new { c.BrandProfileId, c.Status });

        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}

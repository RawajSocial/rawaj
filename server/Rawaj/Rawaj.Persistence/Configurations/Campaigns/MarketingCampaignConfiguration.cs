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

        builder.HasOne(c => c.BrandProfile)
            .WithMany(b => b.Campaigns)
            .HasForeignKey(c => c.BrandProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(c => c.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

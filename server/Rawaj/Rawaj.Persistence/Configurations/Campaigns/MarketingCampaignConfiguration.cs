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

        builder.Property(c => c.Name).HasMaxLength(255);
        builder.Property(c => c.TargetPlatforms).HasJsonConversion().HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(c => c.BudgetAmount).HasColumnType("decimal(12,2)");
        builder.Property(c => c.BudgetCurrency).HasMaxLength(5);
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(c => c.AiPlanJson).HasColumnType("nvarchar(max)");

        builder.Property(c => c.CampaignType).HasMaxLength(30).IsRequired();
        builder.Property(c => c.CampaignBrief).HasJsonConversion().HasColumnType("nvarchar(max)");

        builder.Property(c => c.BrandWords).HasJsonConversion().HasColumnType("nvarchar(max)");
        builder.Property(c => c.BrandTone).HasJsonConversion().HasColumnType("nvarchar(max)");
        builder.Property(c => c.BrandColors).HasJsonConversion().HasColumnType("nvarchar(max)");
        builder.Property(c => c.ContentLanguages).HasJsonConversion().HasColumnType("nvarchar(max)");
        builder.Property(c => c.ExistingPlatforms).HasJsonConversion().HasColumnType("nvarchar(max)");

        builder.Property(c => c.AgeRanges).HasJsonConversion().HasColumnType("nvarchar(max)");
        builder.Property(c => c.IncomeLevel).HasJsonConversion().HasColumnType("nvarchar(max)");
        builder.Property(c => c.Interests).HasJsonConversion().HasColumnType("nvarchar(max)");
        builder.Property(c => c.BuyingBehavior).HasJsonConversion().HasColumnType("nvarchar(max)");
        builder.Property(c => c.AudiencePlatforms).HasJsonConversion().HasColumnType("nvarchar(max)");

        builder.Property(c => c.SuccessMetrics).HasJsonConversion().HasColumnType("nvarchar(max)");
        builder.Property(c => c.BrandsAdmired).HasJsonConversion().HasColumnType("nvarchar(max)");
        builder.Property(c => c.PlatformRanking).HasJsonConversion().HasColumnType("nvarchar(max)");
        builder.Property(c => c.Goals).HasJsonConversion().HasColumnType("nvarchar(max)");
        builder.Property(c => c.BudgetFrom).HasColumnType("decimal(12,2)");
        builder.Property(c => c.BudgetTo).HasColumnType("decimal(12,2)");

        builder.Property(c => c.CampaignPhotoUrls).HasJsonConversion().HasColumnType("nvarchar(max)");
        builder.Property(c => c.StrategistAnswers).HasJsonConversion().HasColumnType("nvarchar(max)");

        builder.HasIndex(c => new { c.BrandProfileId, c.Status, c.CreatedAt });

        builder.HasOne(c => c.BrandProfile)
            .WithMany()
            .HasForeignKey(c => c.BrandProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(c => c.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

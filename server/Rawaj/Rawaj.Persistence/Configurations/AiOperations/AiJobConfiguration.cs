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

        builder.HasOne(a => a.BrandProfile)
            .WithMany()
            .HasForeignKey(a => a.BrandProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(a => a.TriggeredBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

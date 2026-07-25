using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rawaj.Domain.Entities.Platform;
using Rawaj.Persistence.Identity;

namespace Rawaj.Persistence.Configurations.Platform;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Type).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(n => n.Category).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(n => n.Title).HasMaxLength(255).IsRequired();
        builder.Property(n => n.Message).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(n => n.RefType).HasMaxLength(100);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(n => new { n.UserId, n.IsRead });
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rawaj.Domain.Entities.Identity;
using Rawaj.Persistence.Identity;

namespace Rawaj.Persistence.Configurations.Identity;

public class EmailOtpConfiguration : IEntityTypeConfiguration<EmailOtp>
{
    public void Configure(EntityTypeBuilder<EmailOtp> builder)
    {
        builder.ToTable("email_otps");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Purpose).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(o => o.CodeHash).HasMaxLength(128).IsRequired();

        builder.HasIndex(o => new { o.UserId, o.Purpose }).IsUnique();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

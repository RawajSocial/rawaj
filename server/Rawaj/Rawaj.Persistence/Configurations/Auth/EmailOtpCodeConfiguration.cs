using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rawaj.Domain.Entities.Auth;
using Rawaj.Persistence.Identity;

namespace Rawaj.Persistence.Configurations.Auth;

public class EmailOtpCodeConfiguration : IEntityTypeConfiguration<EmailOtpCode>
{
    public void Configure(EntityTypeBuilder<EmailOtpCode> builder)
    {
        builder.ToTable("email_otp_codes");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Email).HasMaxLength(255).IsRequired();
        builder.Property(o => o.CodeHash).HasMaxLength(128).IsRequired();

        builder.HasIndex(o => new { o.UserId, o.ConsumedAt });

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

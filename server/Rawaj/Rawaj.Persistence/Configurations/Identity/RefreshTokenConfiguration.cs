using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rawaj.Domain.Entities.Identity;
using Rawaj.Persistence.Identity;

namespace Rawaj.Persistence.Configurations.Identity;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.TokenHash).HasMaxLength(128).IsRequired();
        builder.HasIndex(t => t.TokenHash).IsUnique();

        builder.Property(t => t.CreatedByIp).HasMaxLength(64);

        builder.HasIndex(t => new { t.UserId, t.RevokedAt });

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

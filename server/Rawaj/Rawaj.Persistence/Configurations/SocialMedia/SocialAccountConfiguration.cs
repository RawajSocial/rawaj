using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rawaj.Domain.Entities.SocialMedia;
using Rawaj.Persistence.Common;

namespace Rawaj.Persistence.Configurations.SocialMedia;

public class SocialAccountConfiguration : IEntityTypeConfiguration<SocialAccount>
{
    public void Configure(EntityTypeBuilder<SocialAccount> builder)
    {
        builder.ToTable("social_accounts");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Type).HasMaxLength(50);
        builder.Property(s => s.Platform).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(s => s.AccountName).HasMaxLength(255).IsRequired();
        builder.Property(s => s.AccountIdExternal).HasMaxLength(255).IsRequired();
        builder.Property(s => s.Token).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(s => s.RefreshTokenEnc).HasColumnType("nvarchar(max)");
        builder.Property(s => s.Scopes).HasJsonConversion().HasColumnType("nvarchar(max)");

        builder.HasOne(s => s.BrandProfile)
            .WithMany()
            .HasForeignKey(s => s.BrandProfileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

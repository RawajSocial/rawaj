using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Persistence.Identity;

namespace Rawaj.Persistence.Configurations.Tenants;

public class AccountSetupConfiguration : IEntityTypeConfiguration<AccountSetup>
{
    public void Configure(EntityTypeBuilder<AccountSetup> builder)
    {
        builder.ToTable("account_setups");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.CompletedSteps).HasMaxLength(500).IsRequired();
        builder.Ignore(a => a.IsCompleted);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => a.UserId).IsUnique();
    }
}

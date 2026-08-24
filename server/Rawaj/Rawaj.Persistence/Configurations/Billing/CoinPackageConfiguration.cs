using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rawaj.Domain.Entities.Billing;

namespace Rawaj.Persistence.Configurations.Billing;

public class CoinPackageConfiguration : IEntityTypeConfiguration<CoinPackage>
{
    public static readonly Guid StarterId = Guid.Parse("00000000-0000-0000-0000-000000000101");
    public static readonly Guid GrowthId = Guid.Parse("00000000-0000-0000-0000-000000000102");
    public static readonly Guid BusinessId = Guid.Parse("00000000-0000-0000-0000-000000000103");
    public static readonly Guid EnterpriseId = Guid.Parse("00000000-0000-0000-0000-000000000104");

    private static readonly DateTime SeedCreatedAt = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<CoinPackage> builder)
    {
        builder.ToTable("coin_packages");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).HasMaxLength(100).IsRequired();
        builder.Property(p => p.PriceUsd).HasColumnType("decimal(10,2)").IsRequired();

        builder.HasData(
            new CoinPackage { Id = StarterId, Name = "Starter", Coins = 5000, BonusCoins = 0, PriceUsd = 50, IsActive = true, CreatedAt = SeedCreatedAt },
            new CoinPackage { Id = GrowthId, Name = "Growth", Coins = 10000, BonusCoins = 1000, PriceUsd = 100, IsActive = true, CreatedAt = SeedCreatedAt },
            new CoinPackage { Id = BusinessId, Name = "Business", Coins = 20000, BonusCoins = 3000, PriceUsd = 200, IsActive = true, CreatedAt = SeedCreatedAt },
            new CoinPackage { Id = EnterpriseId, Name = "Enterprise", Coins = 40000, BonusCoins = 8000, PriceUsd = 400, IsActive = true, CreatedAt = SeedCreatedAt });
    }
}

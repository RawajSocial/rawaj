using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rawaj.Domain.Entities.Tenants;

namespace Rawaj.Persistence.Configurations.Tenants;

public class TenantMemberBrandAccessConfiguration : IEntityTypeConfiguration<TenantMemberBrandAccess>
{
    public void Configure(EntityTypeBuilder<TenantMemberBrandAccess> builder)
    {
        builder.ToTable("tenant_member_brand_access");

        builder.HasKey(a => a.Id);

        builder.HasOne(a => a.TenantMember)
            .WithMany(m => m.BrandAccesses)
            .HasForeignKey(a => a.TenantMemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.BrandProfile)
            .WithMany()
            .HasForeignKey(a => a.BrandProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.TenantMemberId, a.BrandProfileId }).IsUnique();
    }
}

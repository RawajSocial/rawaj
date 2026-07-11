using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Persistence.Identity;

namespace Rawaj.Persistence.Configurations.Campaigns;

public class ContentRevisionConfiguration : IEntityTypeConfiguration<ContentRevision>
{
    public void Configure(EntityTypeBuilder<ContentRevision> builder)
    {
        builder.ToTable("content_revisions");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.RevisionPrompt).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(r => r.Previous).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(r => r.Current).HasColumnType("nvarchar(max)").IsRequired();

        builder.HasOne(r => r.ContentItem)
            .WithMany(c => c.Revisions)
            .HasForeignKey(r => r.ContentItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(r => r.RevisedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

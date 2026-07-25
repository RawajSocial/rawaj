using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rawaj.Domain.Entities.Platform;

namespace Rawaj.Persistence.Configurations.Platform;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Type).HasMaxLength(100).IsRequired();
        builder.Property(o => o.PayloadJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(o => o.LastError).HasColumnType("nvarchar(max)");

        // The dispatcher's poll query filters on ProcessedAt IS NULL, ordered by CreatedAt.
        builder.HasIndex(o => new { o.ProcessedAt, o.CreatedAt });
    }
}

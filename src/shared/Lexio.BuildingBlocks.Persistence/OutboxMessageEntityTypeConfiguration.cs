using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lexio.BuildingBlocks.Persistence;

/// <summary>EF Core configuration for the outbox_messages table.</summary>
internal sealed class OutboxMessageEntityTypeConfiguration : IEntityTypeConfiguration<OutboxMessageEntity>
{
    public void Configure(EntityTypeBuilder<OutboxMessageEntity> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Type).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Payload).IsRequired();
        builder.Property(x => x.OccurredAt).IsRequired();
        builder.HasIndex(x => x.ProcessedAt); // for efficient pending-message queries
    }
}

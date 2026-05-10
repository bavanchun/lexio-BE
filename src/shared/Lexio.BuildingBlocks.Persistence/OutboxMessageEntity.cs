namespace Lexio.BuildingBlocks.Persistence;

/// <summary>
/// EF Core entity mapped to the outbox_messages table.
/// Separate from the Abstractions OutboxMessage record to allow EF configuration.
/// </summary>
public sealed class OutboxMessageEntity
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
}

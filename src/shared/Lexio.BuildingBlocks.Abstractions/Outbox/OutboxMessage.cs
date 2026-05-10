namespace Lexio.BuildingBlocks.Abstractions.Outbox;

/// <summary>
/// Persisted representation of a domain event waiting to be published via the outbox pattern.
/// Written atomically with the aggregate state change in IUnitOfWork.SaveChangesAsync.
/// </summary>
/// <param name="Id">Unique message identifier (used for idempotency on retry).</param>
/// <param name="Type">Fully-qualified CLR type name of the original domain event.</param>
/// <param name="Payload">JSON-serialized event payload.</param>
/// <param name="OccurredAt">UTC timestamp from the original domain event.</param>
/// <param name="ProcessedAt">Set by the outbox processor once the event is dispatched; null = pending.</param>
public sealed record OutboxMessage(
    Guid Id,
    string Type,
    string Payload,
    DateTimeOffset OccurredAt,
    DateTimeOffset? ProcessedAt);

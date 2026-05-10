namespace Lexio.BuildingBlocks.Abstractions.Outbox;

/// <summary>
/// Transactional outbox — append domain events alongside the aggregate write,
/// then dispatch them asynchronously without 2PC.
/// </summary>
public interface IOutbox
{
    /// <summary>
    /// Append an outbox message within the current unit-of-work transaction.
    /// Must be called before SaveChangesAsync to participate in the same commit.
    /// </summary>
    Task AppendAsync(OutboxMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dispatch all pending (unprocessed) outbox messages and mark them processed.
    /// Invoked by a background worker (e.g. Hangfire / hosted service) — not inline.
    /// </summary>
    Task DispatchPendingAsync(CancellationToken cancellationToken = default);
}

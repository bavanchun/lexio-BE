namespace Lexio.BuildingBlocks.Abstractions.Persistence;

/// <summary>
/// Unit-of-work boundary. Callers commit all pending aggregate changes,
/// domain event dispatch, and outbox writes in a single database transaction.
///
/// IMPORTANT: Do NOT bypass this by calling DbContext.SaveChangesAsync directly —
/// the outbox + domain-event dispatch is wired inside the implementation.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Persist all pending changes, publish domain events to the outbox,
    /// and stamp audit fields — atomically.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

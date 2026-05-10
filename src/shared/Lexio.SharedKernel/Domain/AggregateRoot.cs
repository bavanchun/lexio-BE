using Lexio.SharedKernel.Events;

namespace Lexio.SharedKernel.Domain;

/// <summary>
/// Base class for aggregate roots. Extends Entity with domain event collection.
/// Callers must invoke <see cref="ClearDomainEvents"/> after dispatching events
/// (typically inside IUnitOfWork.SaveChangesAsync).
/// </summary>
/// <typeparam name="TId">Strongly-typed aggregate identifier.</typeparam>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>Snapshot of uncommitted domain events. Read-only externally.</summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected AggregateRoot() { }

    protected AggregateRoot(TId id) : base(id) { }

    /// <summary>Enqueue a domain event to be dispatched after persistence.</summary>
    protected void Raise(IDomainEvent @event) => _domainEvents.Add(@event);

    /// <summary>
    /// Remove all queued events. Called by the Unit-of-Work after events are
    /// persisted to the outbox table.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}

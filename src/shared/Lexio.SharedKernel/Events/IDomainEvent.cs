namespace Lexio.SharedKernel.Events;

/// <summary>
/// Marker interface for all domain events raised by aggregate roots.
/// Locked in phase 03 — any change is breaking for all services.
/// </summary>
public interface IDomainEvent
{
    /// <summary>Unique event identifier.</summary>
    Guid Id { get; }

    /// <summary>UTC timestamp when the event occurred.</summary>
    DateTimeOffset OccurredAt { get; }
}

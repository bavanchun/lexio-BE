namespace Lexio.BuildingBlocks.Abstractions.Messaging;

/// <summary>
/// Marker interface for integration events that cross service boundaries via the event bus.
/// Contrast with IDomainEvent (intra-service, raised by aggregates).
/// </summary>
public interface IIntegrationEvent
{
    /// <summary>Unique event identifier for idempotency checks.</summary>
    Guid Id { get; }

    /// <summary>UTC timestamp when the event was created.</summary>
    DateTimeOffset OccurredAt { get; }
}

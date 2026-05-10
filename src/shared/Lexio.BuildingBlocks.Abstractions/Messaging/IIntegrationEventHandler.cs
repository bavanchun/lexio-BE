namespace Lexio.BuildingBlocks.Abstractions.Messaging;

/// <summary>
/// Handler contract for consuming integration events from the event bus.
/// Implementations live in each service's Application layer.
/// </summary>
/// <typeparam name="TEvent">The integration event type to handle.</typeparam>
public interface IIntegrationEventHandler<in TEvent>
    where TEvent : IIntegrationEvent
{
    /// <summary>Process the received integration event.</summary>
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}

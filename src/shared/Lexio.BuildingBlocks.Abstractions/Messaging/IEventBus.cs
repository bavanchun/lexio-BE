namespace Lexio.BuildingBlocks.Abstractions.Messaging;

/// <summary>
/// Abstracts the integration event bus (MassTransit + RabbitMQ in phase 05).
/// Wrapping behind this interface means the transport can be swapped without
/// touching service Application/Domain layers.
/// </summary>
public interface IEventBus
{
    /// <summary>Publish an integration event to the bus.</summary>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent;
}

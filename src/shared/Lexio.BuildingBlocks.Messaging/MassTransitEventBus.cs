using Lexio.BuildingBlocks.Abstractions.Messaging;
using MassTransit;

namespace Lexio.BuildingBlocks.Messaging;

/// <summary>
/// IEventBus implementation backed by MassTransit 8.x (RabbitMQ transport).
/// Wrapping behind IEventBus means the transport can be swapped without touching
/// service Application/Domain layers.
/// </summary>
internal sealed class MassTransitEventBus(IPublishEndpoint publishEndpoint) : IEventBus
{
    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent =>
        publishEndpoint.Publish(@event, cancellationToken);
}

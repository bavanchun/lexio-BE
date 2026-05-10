using Confluent.Kafka;
using System.Text.Json;

namespace Lexio.BuildingBlocks.Messaging;

/// <summary>
/// Thin Kafka producer wrapper for streaming integration events (analytics / event-sourcing streams).
/// Use IEventBus (MassTransit/RabbitMQ) for command-style work queues; use this for high-throughput streams.
/// </summary>
public sealed class KafkaEventPublisher(IProducer<string, string> producer) : IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Publish an event payload to a Kafka topic.</summary>
    public async Task PublishAsync<T>(string topic, string key, T payload, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        await producer.ProduceAsync(topic, new Message<string, string> { Key = key, Value = json }, cancellationToken)
            .ConfigureAwait(false);
    }

    public ValueTask DisposeAsync()
    {
        producer.Flush(TimeSpan.FromSeconds(5));
        producer.Dispose();
        return ValueTask.CompletedTask;
    }
}

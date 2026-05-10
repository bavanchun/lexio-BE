using Testcontainers.RabbitMq;

namespace Lexio.TestUtils.Fixtures;

/// <summary>
/// xUnit v3 async lifetime fixture that starts a RabbitMQ 3.13 container.
/// Use via [Collection] to share a single container across tests in the same class.
/// </summary>
public sealed class RabbitMqFixture : IAsyncLifetime
{
    /// <summary>The running RabbitMQ container.</summary>
    public RabbitMqContainer Container { get; } = new RabbitMqBuilder()
        .WithImage("rabbitmq:3.13-management-alpine")
        .Build();

    /// <summary>Connection string (AMQP URI) for the running container.</summary>
    public string ConnectionString => Container.GetConnectionString();

    /// <inheritdoc />
    public ValueTask InitializeAsync() => new(Container.StartAsync());

    /// <inheritdoc />
    public ValueTask DisposeAsync() => Container.DisposeAsync();
}

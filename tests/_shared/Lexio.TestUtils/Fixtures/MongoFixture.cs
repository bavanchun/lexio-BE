using Testcontainers.MongoDb;

namespace Lexio.TestUtils.Fixtures;

/// <summary>
/// xUnit v3 async lifetime fixture that starts a MongoDB 8 container.
/// Use via [Collection] to share a single container across tests in the same class.
/// </summary>
public sealed class MongoFixture : IAsyncLifetime
{
    /// <summary>The running MongoDB container.</summary>
    public MongoDbContainer Container { get; } = new MongoDbBuilder()
        .WithImage("mongo:8")
        .Build();

    /// <summary>Connection string for the running container.</summary>
    public string ConnectionString => Container.GetConnectionString();

    /// <inheritdoc />
    public ValueTask InitializeAsync() => new(Container.StartAsync());

    /// <inheritdoc />
    public ValueTask DisposeAsync() => Container.DisposeAsync();
}

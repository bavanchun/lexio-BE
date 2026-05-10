using Testcontainers.Redis;

namespace Lexio.TestUtils.Fixtures;

/// <summary>
/// xUnit v3 async lifetime fixture that starts a Redis 7 container.
/// Use via [Collection] to share a single container across tests in the same class.
/// </summary>
public sealed class RedisFixture : IAsyncLifetime
{
    /// <summary>The running Redis container.</summary>
    public RedisContainer Container { get; } = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();

    /// <summary>Connection string for the running container.</summary>
    public string ConnectionString => Container.GetConnectionString();

    /// <inheritdoc />
    public ValueTask InitializeAsync() => new(Container.StartAsync());

    /// <inheritdoc />
    public ValueTask DisposeAsync() => Container.DisposeAsync();
}

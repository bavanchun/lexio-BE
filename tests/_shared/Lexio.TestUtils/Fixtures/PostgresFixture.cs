using Testcontainers.PostgreSql;

namespace Lexio.TestUtils.Fixtures;

/// <summary>
/// xUnit v3 async lifetime fixture that starts a PostgreSQL 17 container.
/// Use via [Collection] to share a single container across tests in the same class.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    /// <summary>The running PostgreSQL container.</summary>
    public PostgreSqlContainer Container { get; } = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .Build();

    /// <summary>Connection string for the running container.</summary>
    public string ConnectionString => Container.GetConnectionString();

    /// <inheritdoc />
    public ValueTask InitializeAsync() => new(Container.StartAsync());

    /// <inheritdoc />
    public ValueTask DisposeAsync() => Container.DisposeAsync();
}

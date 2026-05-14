using Lexio.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lexio.Identity.Infrastructure.Persistence;

/// <summary>
/// Allows <c>dotnet ef migrations add</c> to instantiate the context without DI.
/// Connection string is irrelevant for generating migrations — Npgsql parses but
/// never connects.
/// </summary>
public sealed class IdentityDbContextDesignTimeFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(
                "Host=localhost;Port=5432;Database=identity_db;Username=lexio;Password=devpass",
                npg => npg.SetPostgresVersion(18, 0)
                          .MigrationsHistoryTable("__ef_migrations_history", "public"))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new IdentityDbContext(options, new DesignTimeClock());
    }

    private sealed class DesignTimeClock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}

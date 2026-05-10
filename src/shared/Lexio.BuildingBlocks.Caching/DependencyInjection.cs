using Lexio.BuildingBlocks.Abstractions.Caching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Lexio.BuildingBlocks.Caching;

/// <summary>Service registration extension for Caching building block.</summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers IConnectionMultiplexer (singleton) and ILexioCache.
    /// Reads Redis:ConnectionString from configuration.
    /// </summary>
    public static IServiceCollection AddLexioCaching(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration["Redis:ConnectionString"]
            ?? throw new InvalidOperationException("Redis:ConnectionString is required.");

        services.AddSingleton<IConnectionMultiplexer>(
            _ => ConnectionMultiplexer.Connect(connectionString));

        services.AddSingleton<ILexioCache, RedisLexioCache>();

        return services;
    }
}

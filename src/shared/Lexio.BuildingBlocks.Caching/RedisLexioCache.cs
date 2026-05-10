using System.Text.Json;
using Lexio.BuildingBlocks.Abstractions.Caching;
using StackExchange.Redis;

namespace Lexio.BuildingBlocks.Caching;

/// <summary>
/// Redis-backed implementation of ILexioCache.
/// Serializes values as UTF-8 JSON via System.Text.Json.
/// </summary>
internal sealed class RedisLexioCache(IConnectionMultiplexer multiplexer) : ILexioCache
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IDatabase _db = multiplexer.GetDatabase();

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        where T : class
    {
        var value = await _db.StringGetAsync(key).ConfigureAwait(false);
        if (value.IsNullOrEmpty) { return null; }

        // Explicit string cast to resolve ambiguity between ReadOnlySpan<byte> and string overloads
        return JsonSerializer.Deserialize<T>((string)value!, JsonOptions);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default)
        where T : class
    {
        var json = JsonSerializer.Serialize(value, JsonOptions);
        await _db.StringSetAsync(key, json, ttl).ConfigureAwait(false);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await _db.KeyDeleteAsync(key).ConfigureAwait(false);
    }
}

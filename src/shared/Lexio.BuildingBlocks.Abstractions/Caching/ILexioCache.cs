namespace Lexio.BuildingBlocks.Abstractions.Caching;

/// <summary>
/// Typed distributed cache facade. Hides byte-array serialization from callers.
/// Implementation uses Redis via StackExchange.Redis (BuildingBlocks.Caching phase 05).
/// </summary>
public interface ILexioCache
{
    /// <summary>
    /// Retrieve a cached value by key. Returns null when the key is absent or expired.
    /// </summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Store a value with an absolute expiry TTL. Overwrites any existing key.
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Remove a key from the cache. No-op when key is absent.</summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}

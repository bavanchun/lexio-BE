using Lexio.Identity.Application.Contracts.Persistence;
using Lexio.Identity.Application.Contracts.Security;
using Lexio.Identity.Domain.Primitives;
using Microsoft.Extensions.Caching.Memory;

namespace Lexio.Identity.Api.Authorization;

/// <summary>
/// 60s in-process cache of "is this user banned" answers. Backs the per-write-request
/// DB-authoritative ban check; multi-instance deployments accept ≤60s of staleness.
/// </summary>
public sealed class BanStatusCache(IMemoryCache cache, IUserRepository users) : IBanStatusCache
{
    public static readonly TimeSpan Ttl = TimeSpan.FromSeconds(60);

    public async Task<bool> IsBannedAsync(UserId userId, CancellationToken ct = default) =>
        await cache.GetOrCreateAsync(KeyFor(userId), entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = Ttl;
            return users.IsBannedAsync(userId, ct);
        }).ConfigureAwait(false);

    public void Invalidate(UserId userId) => cache.Remove(KeyFor(userId));

    private static string KeyFor(UserId userId) => $"ban:{userId.Value}";
}

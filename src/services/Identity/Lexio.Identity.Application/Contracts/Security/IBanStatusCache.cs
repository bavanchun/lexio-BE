using Lexio.Identity.Domain.Primitives;

namespace Lexio.Identity.Application.Contracts.Security;

/// <summary>
/// Invalidation hook for the Api-layer ban-status cache. Implemented in
/// Lexio.Identity.Api; Application calls <see cref="Invalidate"/> from the
/// ChangeRole flow so subsequent writes observe the new ban state immediately
/// instead of after the cache TTL.
/// </summary>
public interface IBanStatusCache
{
    void Invalidate(UserId userId);
}

using Lexio.Identity.Domain.Entities;

namespace Lexio.Identity.Application.Contracts.Security;

public sealed record IssuedAccessToken(string Jwt, DateTimeOffset ExpiresAt, int ExpiresInSeconds);

public sealed record IssuedRefreshToken(string RawToken, string HashedForStorage, DateTimeOffset ExpiresAt);

public interface ITokenIssuer
{
    /// <summary>Mints an RS256 JWT carrying sub, email, name, role, permissions, banned.</summary>
    IssuedAccessToken IssueAccessToken(User user, Role role);

    /// <summary>Generates a cryptographically random refresh token + its storage hash.</summary>
    IssuedRefreshToken IssueRefreshToken();

    /// <summary>
    /// Deterministic, fast hash used for refresh-token lookup. Same component owns issue
    /// and lookup so storage and lookup hashes always agree. Must NOT be a salted KDF.
    /// </summary>
    string ComputeStorageHash(string rawToken);
}

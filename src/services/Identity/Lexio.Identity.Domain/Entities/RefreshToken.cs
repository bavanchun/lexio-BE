using Lexio.Identity.Domain.Primitives;
using Lexio.SharedKernel.Domain;
using Lexio.SharedKernel.Time;

namespace Lexio.Identity.Domain.Entities;

/// <summary>
/// One refresh token issued to a user. Stored as a bcrypt hash; raw token never persisted.
/// Rotation uses a 30-second grace window: <see cref="Revoke"/> sets
/// <see cref="RevokedAt"/> to <c>now + 30s</c> so in-flight requests can complete.
/// </summary>
public sealed class RefreshToken : Entity<RefreshTokenId>
{
    /// <summary>Grace window between rotation and effective revocation.</summary>
    public static readonly TimeSpan RotationGrace = TimeSpan.FromSeconds(30);

    public UserId UserId { get; private set; }
    public string TokenHash { get; private set; } = default!;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset IssuedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public string? IpAddress { get; private set; }

    // EF
    private RefreshToken() { }

    private RefreshToken(
        RefreshTokenId id,
        UserId userId,
        string tokenHash,
        DateTimeOffset expiresAt,
        DateTimeOffset issuedAt,
        string? ipAddress) : base(id)
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        IssuedAt = issuedAt;
        IpAddress = ipAddress;
    }

    internal static RefreshToken Issue(
        UserId userId,
        string tokenHash,
        DateTimeOffset expiresAt,
        IClock clock,
        string? ipAddress) =>
        new(RefreshTokenId.New(), userId, tokenHash, expiresAt, clock.UtcNow, ipAddress);

    /// <summary>
    /// Schedule revocation. Sets <see cref="RevokedAt"/> to <c>now + 30s</c> on first call;
    /// idempotent on subsequent calls (does not extend the window).
    /// </summary>
    public void Revoke(IClock clock)
    {
        if (RevokedAt is not null) { return; }
        RevokedAt = clock.UtcNow + RotationGrace;
    }

    /// <summary>True iff the token is unexpired AND its scheduled revoke-time is in the future.</summary>
    public bool IsActive(IClock clock)
    {
        var now = clock.UtcNow;
        if (now >= ExpiresAt) { return false; }
        if (RevokedAt is { } r && now >= r) { return false; }
        return true;
    }
}

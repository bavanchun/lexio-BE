using Lexio.Identity.Domain.Enums;
using Lexio.Identity.Domain.Primitives;
using Lexio.SharedKernel.Domain;
using Lexio.SharedKernel.Time;

namespace Lexio.Identity.Domain.Entities;

/// <summary>
/// Link between a Lexio user and an external identity provider account.
/// Schema-only in MVP
/// </summary>
public sealed class OAuthConnection : Entity<OAuthConnectionId>
{
    public UserId UserId { get; private set; }
    public OAuthProvider Provider { get; private set; }
    public string ProviderUserId { get; private set; } = default!;
    public DateTimeOffset ConnectedAt { get; private set; }
    public DateTimeOffset? LastUsedAt { get; private set; }

    // EF
    private OAuthConnection() { }

    private OAuthConnection(
        OAuthConnectionId id,
        UserId userId,
        OAuthProvider provider,
        string providerUserId,
        DateTimeOffset connectedAt) : base(id)
    {
        UserId = userId;
        Provider = provider;
        ProviderUserId = providerUserId;
        ConnectedAt = connectedAt;
    }

    public static OAuthConnection Create(
        UserId userId,
        OAuthProvider provider,
        string providerUserId,
        IClock clock) =>
        new(OAuthConnectionId.New(), userId, provider, providerUserId, clock.UtcNow);

    public void RecordUse(IClock clock) => LastUsedAt = clock.UtcNow;
}

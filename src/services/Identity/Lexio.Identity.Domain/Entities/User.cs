using Lexio.BuildingBlocks.Abstractions.Persistence;
using Lexio.Identity.Domain.Enums;
using Lexio.Identity.Domain.Events;
using Lexio.Identity.Domain.Exceptions;
using Lexio.Identity.Domain.Primitives;
using Lexio.Identity.Domain.ValueObjects;
using Lexio.SharedKernel.Domain;
using Lexio.SharedKernel.Time;

namespace Lexio.Identity.Domain.Entities;

/// <summary>
/// User aggregate root. Owns its <see cref="RefreshTokens"/> collection — only paths
/// in/out of the collection are this aggregate's methods. <see cref="OAuthConnections"/>
/// is owned similarly.
/// </summary>
public sealed class User : AggregateRoot<UserId>, IAuditableEntity, ISoftDeletableEntity
{
    private readonly List<RefreshToken> _refreshTokens = [];
    private readonly List<OAuthConnection> _oauthConnections = [];

    public Email Email { get; private set; } = default!;
    public PasswordHash? PasswordHash { get; private set; }
    public DisplayName DisplayName { get; private set; } = default!;
    public RoleId RoleId { get; private set; }
    public UserStatus Status { get; private set; }
    public bool IsVerified { get; private set; }
    public DateTimeOffset? EmailVerifiedAt { get; private set; }
    public DateTimeOffset? LastLoginAt { get; private set; }
    public string? BannedReason { get; private set; }
    public DateTimeOffset? BannedAt { get; private set; }

    public IReadOnlyList<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();
    public IReadOnlyList<OAuthConnection> OAuthConnections => _oauthConnections.AsReadOnly();

    // IAuditableEntity
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }

    // ISoftDeletableEntity
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    // EF
    private User() { }

    private User(
        UserId id,
        Email email,
        PasswordHash? passwordHash,
        DisplayName displayName,
        RoleId roleId,
        UserStatus status,
        bool isVerified) : base(id)
    {
        Email = email;
        PasswordHash = passwordHash;
        DisplayName = displayName;
        RoleId = roleId;
        Status = status;
        IsVerified = isVerified;
    }

    /// <summary>
    /// Email/password registration factory. Created users are immediately
    /// <see cref="UserStatus.Active"/>; <c>IsVerified</c> stays false until
    /// the verify-email flow ships and <see cref="VerifyEmail"/> is called.
    /// </summary>
    public static User Register(
        Email email,
        PasswordHash passwordHash,
        DisplayName displayName,
        RoleId defaultRoleId,
        IClock clock)
    {
        var user = new User(
            UserId.New(),
            email,
            passwordHash,
            displayName,
            defaultRoleId,
            UserStatus.Active,
            isVerified: false);

        user.Raise(new UserRegisteredDomainEvent(user.Id, email.Value, displayName.Value, clock.UtcNow));
        return user;
    }

    /// <summary>
    /// External-provider registration factory. Password hash is null;
    /// user authenticates via the linked <see cref="OAuthConnection"/>.
    /// </summary>
    public static User RegisterExternal(
        Email email,
        DisplayName displayName,
        RoleId defaultRoleId,
        OAuthProvider provider,
        string providerUserId,
        IClock clock)
    {
        var user = new User(
            UserId.New(),
            email,
            passwordHash: null,
            displayName,
            defaultRoleId,
            UserStatus.Active,
            isVerified: true);

        user._oauthConnections.Add(OAuthConnection.Create(user.Id, provider, providerUserId, clock));
        user.Raise(new UserRegisteredDomainEvent(user.Id, email.Value, displayName.Value, clock.UtcNow));
        return user;
    }

    public void ChangeDisplayName(DisplayName newName)
    {
        DisplayName = newName;
    }

    public void VerifyEmail(IClock clock)
    {
        if (IsVerified) { return; }
        IsVerified = true;
        EmailVerifiedAt = clock.UtcNow;
    }

    public void RecordLogin(string? ipAddress, IClock clock)
    {
        LastLoginAt = clock.UtcNow;
        Raise(new UserLoggedInDomainEvent(Id, ipAddress, clock.UtcNow));
    }

    public void ChangePassword(PasswordHash newHash, IClock clock)
    {
        PasswordHash = newHash;
        Raise(new PasswordChangedDomainEvent(Id, clock.UtcNow));
    }

    public void ChangeRole(RoleId newRoleId, UserId byAdminId, IClock clock)
    {
        if (Status == UserStatus.Banned)
        {
            throw new UserAlreadyBannedException(Id);
        }
        if (RoleId == newRoleId) { return; }
        var old = RoleId;
        RoleId = newRoleId;
        Raise(new RoleChangedDomainEvent(Id, old, newRoleId, byAdminId, clock.UtcNow));
    }

    public void Ban(string reason, UserId byAdminId, IClock clock)
    {
        if (Status == UserStatus.Banned)
        {
            throw new UserAlreadyBannedException(Id);
        }
        Status = UserStatus.Banned;
        BannedReason = reason;
        BannedAt = clock.UtcNow;
        Raise(new UserBannedDomainEvent(Id, byAdminId, reason, clock.UtcNow));
    }

    public RefreshToken IssueRefreshToken(string tokenHash, DateTimeOffset expiresAt, string? ipAddress, IClock clock)
    {
        var token = RefreshToken.Issue(Id, tokenHash, expiresAt, clock, ipAddress);
        _refreshTokens.Add(token);
        return token;
    }

    public void RevokeRefreshToken(RefreshTokenId tokenId, IClock clock)
    {
        var token = _refreshTokens.FirstOrDefault(t => t.Id == tokenId);
        token?.Revoke(clock);
    }

    public void RevokeAllRefreshTokens(IClock clock)
    {
        foreach (var t in _refreshTokens) { t.Revoke(clock); }
    }
}

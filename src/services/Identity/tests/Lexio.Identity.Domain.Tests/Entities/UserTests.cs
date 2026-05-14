using Lexio.Identity.Domain.Entities;
using Lexio.Identity.Domain.Enums;
using Lexio.Identity.Domain.Events;
using Lexio.Identity.Domain.Exceptions;
using Lexio.Identity.Domain.Primitives;
using Lexio.Identity.Domain.ValueObjects;
using Lexio.TestUtils;

namespace Lexio.Identity.Domain.Tests.Entities;

public class UserTests
{
    private static readonly RoleId LearnerRole = RoleId.New();
    private const string ValidBcrypt = "$2a$12$abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmno1234";

    private static User RegisterTestUser(TestClock? clock = null) =>
        User.Register(
            Email.From("alice@example.com"),
            PasswordHash.From(ValidBcrypt),
            DisplayName.From("Alice"),
            LearnerRole,
            clock ?? new TestClock());

    [Fact]
    public void Register_creates_active_unverified_user_and_raises_event()
    {
        var clock = new TestClock();
        var user = RegisterTestUser(clock);

        user.Status.Should().Be(UserStatus.Active);
        user.IsVerified.Should().BeFalse();
        user.Email.Value.Should().Be("alice@example.com");
        user.RoleId.Should().Be(LearnerRole);
        user.DomainEvents.Should().ContainSingle(e => e is UserRegisteredDomainEvent);
    }

    [Fact]
    public void RegisterExternal_marks_verified_and_links_oauth_connection()
    {
        var user = User.RegisterExternal(
            Email.From("alice@example.com"),
            DisplayName.From("Alice"),
            LearnerRole,
            OAuthProvider.Google,
            "google-sub-123",
            new TestClock());

        user.PasswordHash.Should().BeNull();
        user.IsVerified.Should().BeTrue();
        user.OAuthConnections.Should().HaveCount(1);
        user.OAuthConnections[0].Provider.Should().Be(OAuthProvider.Google);
        user.OAuthConnections[0].ProviderUserId.Should().Be("google-sub-123");
    }

    [Fact]
    public void VerifyEmail_is_idempotent()
    {
        var clock = new TestClock();
        var user = RegisterTestUser(clock);
        user.VerifyEmail(clock);
        var firstVerifiedAt = user.EmailVerifiedAt;
        clock.Advance(TimeSpan.FromHours(1));
        user.VerifyEmail(clock);
        user.EmailVerifiedAt.Should().Be(firstVerifiedAt);
    }

    [Fact]
    public void RecordLogin_stamps_timestamp_and_raises_event()
    {
        var clock = new TestClock();
        var user = RegisterTestUser(clock);
        user.ClearDomainEvents();
        clock.Advance(TimeSpan.FromMinutes(5));

        user.RecordLogin("1.2.3.4", clock);

        user.LastLoginAt.Should().Be(clock.UtcNow);
        user.DomainEvents.OfType<UserLoggedInDomainEvent>().Should().ContainSingle();
    }

    [Fact]
    public void ChangePassword_swaps_hash_and_raises_event()
    {
        var clock = new TestClock();
        var user = RegisterTestUser(clock);
        user.ClearDomainEvents();
        var newHash = PasswordHash.From("$2b$10$differentSaltdifferentSaltdifferentSaltdifferent12345");

        user.ChangePassword(newHash, clock);

        user.PasswordHash.Should().Be(newHash);
        user.DomainEvents.OfType<PasswordChangedDomainEvent>().Should().ContainSingle();
    }

    [Fact]
    public void ChangeRole_to_same_role_is_no_op()
    {
        var user = RegisterTestUser();
        user.ClearDomainEvents();
        user.ChangeRole(user.RoleId, UserId.New(), new TestClock());
        user.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ChangeRole_to_new_role_raises_event_with_old_and_new()
    {
        var clock = new TestClock();
        var user = RegisterTestUser(clock);
        user.ClearDomainEvents();
        var admin = UserId.New();
        var newRole = RoleId.New();

        user.ChangeRole(newRole, admin, clock);

        user.RoleId.Should().Be(newRole);
        var evt = user.DomainEvents.OfType<RoleChangedDomainEvent>().Single();
        evt.OldRoleId.Should().Be(LearnerRole);
        evt.NewRoleId.Should().Be(newRole);
        evt.ChangedByAdminId.Should().Be(admin);
    }

    [Fact]
    public void ChangeRole_throws_when_user_is_banned()
    {
        var user = RegisterTestUser();
        user.Ban("spam", UserId.New(), new TestClock());

        var act = () => user.ChangeRole(RoleId.New(), UserId.New(), new TestClock());
        act.Should().Throw<UserAlreadyBannedException>();
    }

    [Fact]
    public void Ban_sets_status_and_raises_event()
    {
        var clock = new TestClock();
        var user = RegisterTestUser(clock);
        user.ClearDomainEvents();

        user.Ban("abuse", UserId.New(), clock);

        user.Status.Should().Be(UserStatus.Banned);
        user.BannedReason.Should().Be("abuse");
        user.BannedAt.Should().Be(clock.UtcNow);
        user.DomainEvents.OfType<UserBannedDomainEvent>().Should().ContainSingle();
    }

    [Fact]
    public void Ban_when_already_banned_throws()
    {
        var user = RegisterTestUser();
        user.Ban("first", UserId.New(), new TestClock());
        var act = () => user.Ban("second", UserId.New(), new TestClock());
        act.Should().Throw<UserAlreadyBannedException>();
    }

    [Fact]
    public void IssueRefreshToken_appends_to_collection()
    {
        var clock = new TestClock();
        var user = RegisterTestUser(clock);
        var token = user.IssueRefreshToken("hash1", clock.UtcNow.AddDays(7), "1.2.3.4", clock);

        user.RefreshTokens.Should().ContainSingle().Which.Should().Be(token);
        token.UserId.Should().Be(user.Id);
    }

    [Fact]
    public void RevokeRefreshToken_on_unknown_id_is_no_op()
    {
        var user = RegisterTestUser();
        var act = () => user.RevokeRefreshToken(RefreshTokenId.New(), new TestClock());
        act.Should().NotThrow();
    }

    [Fact]
    public void RevokeAllRefreshTokens_revokes_each_token()
    {
        var clock = new TestClock();
        var user = RegisterTestUser(clock);
        user.IssueRefreshToken("h1", clock.UtcNow.AddDays(7), null, clock);
        user.IssueRefreshToken("h2", clock.UtcNow.AddDays(7), null, clock);

        user.RevokeAllRefreshTokens(clock);
        clock.Advance(RefreshToken.RotationGrace);

        user.RefreshTokens.Should().AllSatisfy(t => t.IsActive(clock).Should().BeFalse());
    }

    [Fact]
    public void Aggregate_has_parameterless_ctor_for_ef_materialisation()
    {
        var ctor = typeof(User).GetConstructor(
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            binder: null,
            types: Type.EmptyTypes,
            modifiers: null);
        ctor.Should().NotBeNull("EF Core needs a parameterless ctor to materialise User");
    }
}

using Lexio.Identity.Domain.Entities;
using Lexio.Identity.Domain.Primitives;
using Lexio.Identity.Domain.ValueObjects;
using Lexio.TestUtils;

namespace Lexio.Identity.Domain.Tests.Entities;

public class RefreshTokenTests
{
    private const string ValidBcrypt = "$2a$12$abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmno1234";
    private static readonly RoleId LearnerRole = RoleId.New();

    private static (User user, RefreshToken token, TestClock clock) Setup(TimeSpan? ttl = null)
    {
        var clock = new TestClock();
        var user = User.Register(
            Email.From("alice@example.com"),
            PasswordHash.From(ValidBcrypt),
            DisplayName.From("Alice"),
            LearnerRole,
            clock);
        var token = user.IssueRefreshToken(
            "tokenhash",
            clock.UtcNow.Add(ttl ?? TimeSpan.FromDays(7)),
            "1.2.3.4",
            clock);
        return (user, token, clock);
    }

    [Fact]
    public void Issue_sets_fields_and_active()
    {
        var (_, token, clock) = Setup();
        token.IsActive(clock).Should().BeTrue();
        token.RevokedAt.Should().BeNull();
        token.IpAddress.Should().Be("1.2.3.4");
    }

    [Fact]
    public void Revoke_schedules_effective_revocation_30_seconds_out()
    {
        var (_, token, clock) = Setup();
        token.Revoke(clock);
        token.RevokedAt.Should().Be(clock.UtcNow + RefreshToken.RotationGrace);
    }

    [Fact]
    public void IsActive_within_grace_window_after_revoke()
    {
        var (_, token, clock) = Setup();
        token.Revoke(clock);
        clock.Advance(TimeSpan.FromSeconds(15));
        token.IsActive(clock).Should().BeTrue();
    }

    [Fact]
    public void IsActive_false_after_grace_window()
    {
        var (_, token, clock) = Setup();
        token.Revoke(clock);
        clock.Advance(RefreshToken.RotationGrace);
        token.IsActive(clock).Should().BeFalse();
    }

    [Fact]
    public void IsActive_false_after_expiry()
    {
        var (_, token, clock) = Setup(ttl: TimeSpan.FromHours(1));
        clock.Advance(TimeSpan.FromHours(2));
        token.IsActive(clock).Should().BeFalse();
    }

    [Fact]
    public void Revoke_is_idempotent_and_does_not_extend_window()
    {
        var (_, token, clock) = Setup();
        token.Revoke(clock);
        var first = token.RevokedAt;
        clock.Advance(TimeSpan.FromSeconds(10));
        token.Revoke(clock);
        token.RevokedAt.Should().Be(first);
    }
}

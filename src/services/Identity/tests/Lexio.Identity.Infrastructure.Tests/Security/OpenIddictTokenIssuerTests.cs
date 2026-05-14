using System.IdentityModel.Tokens.Jwt;
using Lexio.Identity.Domain.Entities;
using Lexio.Identity.Domain.Primitives;
using Lexio.Identity.Domain.ValueObjects;
using Lexio.Identity.Infrastructure.Security;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;

namespace Lexio.Identity.Infrastructure.Tests.Security;

public class OpenIddictTokenIssuerTests
{
    private static readonly DateTimeOffset T0 = new(2026, 5, 14, 12, 0, 0, TimeSpan.Zero);

    private static (OpenIddictTokenIssuer issuer, SigningCertificateLoader loader, MutableClock clock) BuildIssuer()
    {
        var env = new Mock<IHostEnvironment>();
        env.SetupGet(e => e.EnvironmentName).Returns(Environments.Development);
        var loader = new SigningCertificateLoader(env.Object);
        var clock = new MutableClock(T0);
        var opts = Options.Create(new OpenIddictTokenIssuerOptions
        {
            Issuer = "https://test/identity",
            Audience = "lexio-test",
            AccessTokenMinutes = 15,
            RefreshTokenDays = 7,
        });
        return (new OpenIddictTokenIssuer(clock, opts, loader), loader, clock);
    }

    private static (User user, Role role) BuildUserAndRole()
    {
        var roleId = new RoleId(Guid.NewGuid());
        var role = Role.Create(roleId, "learner", "Learner role", ["vocab:read", "study:write"]);
        var user = User.Register(
            Email.From("alice@example.com"),
            PasswordHash.From("$2a$12$abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmno1234"),
            DisplayName.From("Alice"),
            roleId,
            new MutableClock(T0));
        return (user, role);
    }

    [Fact]
    public void IssueAccessToken_produces_RS256_JWT_with_expected_claims()
    {
        var (issuer, loader, _) = BuildIssuer();
        var (user, role) = BuildUserAndRole();

        var issued = issuer.IssueAccessToken(user, role);

        var handler = new JwtSecurityTokenHandler();
        var validation = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "https://test/identity",
            ValidateAudience = true,
            ValidAudience = "lexio-test",
            ValidateLifetime = false,
            IssuerSigningKey = new X509SecurityKey(loader.Certificate),
            ValidateIssuerSigningKey = true,
        };

        handler.ValidateToken(issued.Jwt, validation, out var validated);
        var jwt = (JwtSecurityToken)validated;

        jwt.SignatureAlgorithm.Should().Be(SecurityAlgorithms.RsaSha256);
        jwt.Subject.Should().Be(user.Id.Value.ToString());
        jwt.Claims.Should().Contain(c => c.Type == "role" && c.Value == "learner");
        jwt.Claims.Where(c => c.Type == "permissions").Select(c => c.Value)
            .Should().BeEquivalentTo("vocab:read", "study:write");
        jwt.Claims.Should().Contain(c => c.Type == "email" && c.Value == "alice@example.com");
        issued.ExpiresInSeconds.Should().Be(15 * 60);
    }

    [Fact]
    public void IssueRefreshToken_produces_storage_hash_matching_ComputeStorageHash()
    {
        var (issuer, _, _) = BuildIssuer();
        var issued = issuer.IssueRefreshToken();

        issuer.ComputeStorageHash(issued.RawToken).Should().Be(issued.HashedForStorage);
        issued.RawToken.Should().HaveLength(43);
    }

    [Fact]
    public void Refresh_token_grace_window_keeps_token_active_for_30s_after_revoke()
    {
        var clock = new MutableClock(T0);
        var (user, _) = BuildUserAndRole();
        user.IssueRefreshToken("hash", T0.AddDays(7), ipAddress: null, clock);
        var token = user.RefreshTokens.Single();

        token.IsActive(clock).Should().BeTrue();

        user.RevokeRefreshToken(token.Id, clock);

        clock.Advance(TimeSpan.FromSeconds(10));
        token.IsActive(clock).Should().BeTrue("within 30s grace window");

        clock.Advance(TimeSpan.FromSeconds(21));
        token.IsActive(clock).Should().BeFalse("past 30s grace window");
    }

    [Fact]
    public void ComputeStorageHash_is_deterministic_and_base64url()
    {
        var (issuer, _, _) = BuildIssuer();
        var a = issuer.ComputeStorageHash("token-xyz");
        var b = issuer.ComputeStorageHash("token-xyz");
        a.Should().Be(b);
        a.Should().NotContain("=").And.NotContain("/").And.NotContain("+");
    }
}

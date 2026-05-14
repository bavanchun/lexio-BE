using Lexio.BuildingBlocks.Abstractions.Persistence;
using Lexio.Identity.Application.Contracts.Persistence;
using Lexio.Identity.Application.Contracts.Security;
using Lexio.Identity.Application.Features.Auth.Refresh;
using Lexio.Identity.Domain.Entities;
using Lexio.Identity.Domain.Primitives;
using Lexio.Identity.Domain.ValueObjects;
using Lexio.TestUtils;
using Moq;

namespace Lexio.Identity.Application.Tests.Features.Auth;

public class RefreshTokenCommandHandlerTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IRoleRepository> _roles = new();
    private readonly Mock<ITokenIssuer> _tokens = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly TestClock _clock = new();

    private RefreshTokenCommandHandler Handler() =>
        new(_users.Object, _roles.Object, _tokens.Object, _uow.Object, _clock);

    private User MakeUserWithActiveToken(Role role, string tokenHash)
    {
        var user = User.Register(
            Email.From("alice@example.com"),
            PasswordHash.From(TestFactories.ValidBcrypt),
            DisplayName.From("Alice"),
            role.Id,
            _clock);
        user.IssueRefreshToken(tokenHash, _clock.UtcNow.AddDays(7), "1.2.3.4", _clock);
        return user;
    }

    [Fact]
    public async Task Rotates_token_on_happy_path()
    {
        var role = TestFactories.Learner();
        var user = MakeUserWithActiveToken(role, "stored-hash");
        _tokens.Setup(t => t.ComputeStorageHash("raw-token")).Returns("stored-hash");
        _users.Setup(u => u.GetByActiveRefreshTokenHashAsync("stored-hash", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _roles.Setup(r => r.GetByIdAsync(user.RoleId, It.IsAny<CancellationToken>())).ReturnsAsync(role);
        _tokens.Setup(t => t.IssueRefreshToken()).Returns(new IssuedRefreshToken("new-raw", "new-stored", _clock.UtcNow.AddDays(7)));
        _tokens.Setup(t => t.IssueAccessToken(user, role))
            .Returns(new IssuedAccessToken("new-jwt", _clock.UtcNow.AddMinutes(15), 900));

        var result = await Handler().Handle(new RefreshTokenCommand("raw-token", "1.2.3.4"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.RefreshToken.Should().Be("new-raw");
        result.Value.AccessToken.Should().Be("new-jwt");
        var oldToken = user.RefreshTokens.First(t => t.TokenHash == "stored-hash");
        oldToken.RevokedAt.Should().NotBeNull();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Returns_invalid_when_no_active_token_matches()
    {
        _tokens.Setup(t => t.ComputeStorageHash(It.IsAny<string>())).Returns("nope");
        _users.Setup(u => u.GetByActiveRefreshTokenHashAsync("nope", It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var result = await Handler().Handle(new RefreshTokenCommand("rotten-token", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("identity.invalid-refresh-token");
    }

    [Fact]
    public async Task Returns_invalid_when_user_is_banned()
    {
        var role = TestFactories.Learner();
        var user = MakeUserWithActiveToken(role, "stored-hash");
        user.Ban("abuse", UserId.New(), _clock);
        _tokens.Setup(t => t.ComputeStorageHash("raw-token")).Returns("stored-hash");
        _users.Setup(u => u.GetByActiveRefreshTokenHashAsync("stored-hash", It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await Handler().Handle(new RefreshTokenCommand("raw-token", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("identity.invalid-refresh-token");
    }
}

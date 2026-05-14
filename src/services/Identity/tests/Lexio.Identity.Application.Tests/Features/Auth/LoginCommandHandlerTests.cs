using Lexio.BuildingBlocks.Abstractions.Persistence;
using Lexio.Identity.Application.Contracts.Auditing;
using Lexio.Identity.Application.Contracts.Persistence;
using Lexio.Identity.Application.Contracts.Security;
using Lexio.Identity.Application.Features.Auth.Login;
using Lexio.Identity.Application.Features.Auth.Me;
using Lexio.Identity.Domain.Entities;
using Lexio.Identity.Domain.Primitives;
using Lexio.Identity.Domain.ValueObjects;
using Lexio.TestUtils;
using MapsterMapper;
using Moq;

namespace Lexio.Identity.Application.Tests.Features.Auth;

public class LoginCommandHandlerTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IRoleRepository> _roles = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<ITokenIssuer> _tokens = new();
    private readonly Mock<IAuditLogger> _audit = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly TestClock _clock = new();

    private LoginCommandHandler Handler() =>
        new(_users.Object, _roles.Object, _hasher.Object, _tokens.Object,
            _audit.Object, _uow.Object, _clock, _mapper.Object);

    private User MakeUser(Role role) =>
        User.Register(
            Email.From("alice@example.com"),
            PasswordHash.From(TestFactories.ValidBcrypt),
            DisplayName.From("Alice"),
            role.Id,
            _clock);

    [Fact]
    public async Task Returns_success_on_correct_credentials()
    {
        var role = TestFactories.Learner();
        var user = MakeUser(role);
        _users.Setup(u => u.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _roles.Setup(r => r.GetByIdAsync(user.RoleId, It.IsAny<CancellationToken>())).ReturnsAsync(role);
        _hasher.Setup(h => h.Verify("Pass1word!", TestFactories.ValidBcrypt)).Returns(true);
        _tokens.Setup(t => t.IssueRefreshToken()).Returns(new IssuedRefreshToken("raw", "stored", _clock.UtcNow.AddDays(7)));
        _tokens.Setup(t => t.IssueAccessToken(user, role))
            .Returns(new IssuedAccessToken("jwt", _clock.UtcNow.AddMinutes(15), 900));
        _mapper.Setup(m => m.Map<UserDto>(It.IsAny<User>()))
            .Returns(new UserDto(user.Id.Value, "alice@example.com", "Alice", role.Id.Value, "Active", false, _clock.UtcNow, _clock.UtcNow));

        var result = await Handler().Handle(new LoginCommand("alice@example.com", "Pass1word!", "1.2.3.4"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        user.LastLoginAt.Should().NotBeNull();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Returns_generic_invalid_credentials_on_missing_user_and_still_calls_verify()
    {
        // Constant-time defence: even on user-not-found, Verify must run.
        _users.Setup(u => u.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        _hasher.Setup(h => h.Verify(It.IsAny<string>(), null)).Returns(false);

        var result = await Handler().Handle(new LoginCommand("ghost@example.com", "wrong", "1.2.3.4"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("identity.invalid-credentials");
        _hasher.Verify(h => h.Verify("wrong", null), Times.Once);
        _audit.Verify(a => a.LogAsync(It.Is<AuditEvent>(e => e.EventType == "LoginFailed"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Returns_invalid_credentials_on_wrong_password()
    {
        var role = TestFactories.Learner();
        var user = MakeUser(role);
        _users.Setup(u => u.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify("wrong", It.IsAny<string?>())).Returns(false);

        var result = await Handler().Handle(new LoginCommand("alice@example.com", "wrong", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("identity.invalid-credentials");
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Returns_account_banned_when_user_is_banned()
    {
        var role = TestFactories.Learner();
        var user = MakeUser(role);
        user.Ban("spam", UserId.New(), _clock);
        _users.Setup(u => u.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify("Pass1word!", TestFactories.ValidBcrypt)).Returns(true);

        var result = await Handler().Handle(new LoginCommand("alice@example.com", "Pass1word!", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("identity.account-banned");
    }
}

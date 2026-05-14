using Lexio.BuildingBlocks.Abstractions.Persistence;
using Lexio.Identity.Application.Contracts.Auditing;
using Lexio.Identity.Application.Contracts.Persistence;
using Lexio.Identity.Application.Contracts.Security;
using Lexio.Identity.Application.Features.Auth.Me;
using Lexio.Identity.Application.Features.Auth.Register;
using Lexio.Identity.Domain.Entities;
using Lexio.Identity.Domain.ValueObjects;
using Lexio.TestUtils;
using MapsterMapper;
using Moq;

namespace Lexio.Identity.Application.Tests.Features.Auth;

public class RegisterUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IRoleRepository> _roles = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<ITokenIssuer> _tokens = new();
    private readonly Mock<IAuditLogger> _audit = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly TestClock _clock = new();

    private RegisterUserCommandHandler Handler() =>
        new(_users.Object, _roles.Object, _hasher.Object, _tokens.Object,
            _audit.Object, _uow.Object, _clock, _mapper.Object);

    private void SetupHappyPath()
    {
        _users.Setup(u => u.EmailExistsAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _roles.Setup(r => r.GetDefaultLearnerRoleAsync(It.IsAny<CancellationToken>())).ReturnsAsync(TestFactories.Learner());
        _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns(TestFactories.ValidBcrypt);
        _tokens.Setup(t => t.IssueRefreshToken()).Returns(new IssuedRefreshToken("raw", "stored", _clock.UtcNow.AddDays(7)));
        _tokens.Setup(t => t.IssueAccessToken(It.IsAny<User>(), It.IsAny<Role>()))
            .Returns(new IssuedAccessToken("jwt-token", _clock.UtcNow.AddMinutes(15), 900));
        _mapper.Setup(m => m.Map<UserDto>(It.IsAny<User>()))
            .Returns(new UserDto(Guid.NewGuid(), "alice@example.com", "Alice", Guid.NewGuid(), "Active", false, null, _clock.UtcNow));
    }

    [Fact]
    public async Task Returns_success_with_tokens_on_happy_path()
    {
        SetupHappyPath();
        var cmd = new RegisterUserCommand("alice@example.com", "Pass1word!", "Alice", "1.2.3.4");

        var result = await Handler().Handle(cmd, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("jwt-token");
        result.Value.RefreshToken.Should().Be("raw");
        _users.Verify(u => u.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _audit.Verify(a => a.LogAsync(It.Is<AuditEvent>(e => e.EventType == "UserRegistered"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Returns_conflict_when_email_already_exists()
    {
        SetupHappyPath();
        _users.Setup(u => u.EmailExistsAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var cmd = new RegisterUserCommand("alice@example.com", "Pass1word!", "Alice", null);

        var result = await Handler().Handle(cmd, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("identity.email-already-exists");
        _users.Verify(u => u.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Returns_failure_on_invalid_email_format()
    {
        // Validator catches this in pipeline; handler itself also defends via Email.Create.
        var cmd = new RegisterUserCommand("not-an-email", "Pass1word!", "Alice", null);
        var result = await Handler().Handle(cmd, TestContext.Current.CancellationToken);
        result.IsFailure.Should().BeTrue();
    }
}

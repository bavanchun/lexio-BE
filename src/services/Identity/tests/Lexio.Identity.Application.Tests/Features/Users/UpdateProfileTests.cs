using Lexio.BuildingBlocks.Abstractions.Persistence;
using Lexio.Identity.Application.Contracts.Persistence;
using Lexio.Identity.Application.Contracts.Security;
using Lexio.Identity.Application.Features.Auth.Me;
using Lexio.Identity.Application.Features.Users.UpdateProfile;
using Lexio.Identity.Domain.Entities;
using Lexio.Identity.Domain.Primitives;
using Lexio.Identity.Domain.ValueObjects;
using Lexio.TestUtils;
using MapsterMapper;
using Moq;

namespace Lexio.Identity.Application.Tests.Features.Users;

public class UpdateProfileTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly TestClock _clock = new();

    private UpdateProfileCommandHandler Handler() =>
        new(_users.Object, _hasher.Object, _uow.Object, _clock, _mapper.Object);

    private User MakeUser() =>
        User.Register(
            Email.From("alice@example.com"),
            PasswordHash.From(TestFactories.ValidBcrypt),
            DisplayName.From("Alice"),
            RoleId.New(),
            _clock);

    [Fact]
    public async Task Changes_password_when_current_password_matches()
    {
        var user = MakeUser();
        _users.Setup(u => u.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify("OldPass1!", TestFactories.ValidBcrypt)).Returns(true);
        _hasher.Setup(h => h.Hash("NewPass1!")).Returns(TestFactories.AltBcrypt);
        _mapper.Setup(m => m.Map<UserDto>(It.IsAny<User>()))
            .Returns(new UserDto(user.Id.Value, "alice@example.com", "Alice", user.RoleId.Value, "Active", false, null, _clock.UtcNow));

        var result = await Handler().Handle(
            new UpdateProfileCommand(user.Id, null, "OldPass1!", "NewPass1!"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        user.PasswordHash!.Value.Should().Be(TestFactories.AltBcrypt);
    }

    [Fact]
    public async Task Returns_mismatch_when_current_password_wrong()
    {
        var user = MakeUser();
        _users.Setup(u => u.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify("wrong", TestFactories.ValidBcrypt)).Returns(false);

        var result = await Handler().Handle(
            new UpdateProfileCommand(user.Id, null, "wrong", "NewPass1!"),
            TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("identity.current-password-mismatch");
    }

    [Fact]
    public async Task Changes_display_name_only()
    {
        var user = MakeUser();
        _users.Setup(u => u.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _mapper.Setup(m => m.Map<UserDto>(It.IsAny<User>()))
            .Returns(new UserDto(user.Id.Value, "alice@example.com", "Alice Smith", user.RoleId.Value, "Active", false, null, _clock.UtcNow));

        var result = await Handler().Handle(
            new UpdateProfileCommand(user.Id, "Alice Smith", null, null),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        user.DisplayName.Value.Should().Be("Alice Smith");
    }

    [Fact]
    public async Task Returns_user_not_found_when_missing()
    {
        _users.Setup(u => u.GetByIdAsync(It.IsAny<UserId>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var result = await Handler().Handle(
            new UpdateProfileCommand(UserId.New(), "New", null, null),
            TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("identity.user-not-found");
    }
}

using Lexio.BuildingBlocks.Abstractions.Persistence;
using Lexio.Identity.Application.Contracts.Persistence;
using Lexio.Identity.Application.Features.Auth.Logout;
using Lexio.Identity.Application.Features.Auth.Me;
using Lexio.Identity.Domain.Entities;
using Lexio.Identity.Domain.Primitives;
using Lexio.Identity.Domain.ValueObjects;
using Lexio.TestUtils;
using MapsterMapper;
using Moq;

namespace Lexio.Identity.Application.Tests.Features.Auth;

public class LogoutAndMeTests
{
    private readonly TestClock _clock = new();

    private User MakeUser() =>
        User.Register(
            Email.From("alice@example.com"),
            PasswordHash.From(TestFactories.ValidBcrypt),
            DisplayName.From("Alice"),
            TestFactories.Learner().Id,
            _clock);

    [Fact]
    public async Task Logout_revokes_all_active_tokens()
    {
        var user = MakeUser();
        user.IssueRefreshToken("h1", _clock.UtcNow.AddDays(7), null, _clock);
        user.IssueRefreshToken("h2", _clock.UtcNow.AddDays(7), null, _clock);
        var users = new Mock<IUserRepository>();
        users.Setup(u => u.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var uow = new Mock<IUnitOfWork>();

        var result = await new LogoutCommandHandler(users.Object, uow.Object, _clock)
            .Handle(new LogoutCommand(user.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        user.RefreshTokens.Should().AllSatisfy(t => t.RevokedAt.Should().NotBeNull());
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Logout_returns_not_found_when_user_missing()
    {
        var users = new Mock<IUserRepository>();
        users.Setup(u => u.GetByIdAsync(It.IsAny<UserId>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        var uow = new Mock<IUnitOfWork>();

        var result = await new LogoutCommandHandler(users.Object, uow.Object, _clock)
            .Handle(new LogoutCommand(UserId.New()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("identity.user-not-found");
    }

    [Fact]
    public async Task GetMe_returns_dto_when_user_found()
    {
        var user = MakeUser();
        var users = new Mock<IUserRepository>();
        users.Setup(u => u.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var mapper = new Mock<IMapper>();
        var dto = new UserDto(user.Id.Value, "alice@example.com", "Alice", user.RoleId.Value, "Active", false, null, _clock.UtcNow);
        mapper.Setup(m => m.Map<UserDto>(user)).Returns(dto);

        var result = await new GetMeQueryHandler(users.Object, mapper.Object)
            .Handle(new GetMeQuery(user.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(dto);
    }

    [Fact]
    public async Task GetMe_returns_not_found_when_missing()
    {
        var users = new Mock<IUserRepository>();
        users.Setup(u => u.GetByIdAsync(It.IsAny<UserId>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var result = await new GetMeQueryHandler(users.Object, Mock.Of<IMapper>())
            .Handle(new GetMeQuery(UserId.New()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("identity.user-not-found");
    }
}

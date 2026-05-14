using Lexio.BuildingBlocks.Abstractions.Persistence;
using Lexio.Identity.Application.Contracts.Persistence;
using Lexio.Identity.Application.Features.Users.ChangeRole;
using Lexio.Identity.Domain.Entities;
using Lexio.Identity.Domain.Primitives;
using Lexio.Identity.Domain.ValueObjects;
using Lexio.TestUtils;
using Moq;

namespace Lexio.Identity.Application.Tests.Features.Users;

public class ChangeUserRoleTests
{
    private readonly TestClock _clock = new();

    private User MakeUser(RoleId roleId) =>
        User.Register(
            Email.From("alice@example.com"),
            PasswordHash.From(TestFactories.ValidBcrypt),
            DisplayName.From("Alice"),
            roleId,
            _clock);

    [Fact]
    public async Task Changes_role_when_user_and_role_exist()
    {
        var oldRole = TestFactories.Learner();
        var newRole = TestFactories.Learner();
        var user = MakeUser(oldRole.Id);
        var users = new Mock<IUserRepository>();
        users.Setup(u => u.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var roles = new Mock<IRoleRepository>();
        roles.Setup(r => r.GetByIdAsync(newRole.Id, It.IsAny<CancellationToken>())).ReturnsAsync(newRole);
        var uow = new Mock<IUnitOfWork>();

        var result = await new ChangeUserRoleCommandHandler(users.Object, roles.Object, uow.Object, _clock)
            .Handle(new ChangeUserRoleCommand(user.Id, newRole.Id, UserId.New()), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        user.RoleId.Should().Be(newRole.Id);
    }

    [Fact]
    public async Task Returns_role_not_found_when_target_role_missing()
    {
        var user = MakeUser(RoleId.New());
        var users = new Mock<IUserRepository>();
        users.Setup(u => u.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var roles = new Mock<IRoleRepository>();
        roles.Setup(r => r.GetByIdAsync(It.IsAny<RoleId>(), It.IsAny<CancellationToken>())).ReturnsAsync((Role?)null);
        var uow = new Mock<IUnitOfWork>();

        var result = await new ChangeUserRoleCommandHandler(users.Object, roles.Object, uow.Object, _clock)
            .Handle(new ChangeUserRoleCommand(user.Id, RoleId.New(), UserId.New()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("identity.role-not-found");
    }
}

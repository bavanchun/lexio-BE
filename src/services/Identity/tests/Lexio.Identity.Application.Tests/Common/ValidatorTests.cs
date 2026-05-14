using FluentValidation.TestHelper;
using Lexio.Identity.Application.Features.Auth.Login;
using Lexio.Identity.Application.Features.Auth.Refresh;
using Lexio.Identity.Application.Features.Auth.Register;
using Lexio.Identity.Application.Features.Users.UpdateProfile;
using Lexio.Identity.Domain.Primitives;

namespace Lexio.Identity.Application.Tests.Common;

public class ValidatorTests
{
    [Fact]
    public void RegisterUserCommandValidator_rejects_weak_password()
    {
        var v = new RegisterUserCommandValidator();
        v.TestValidate(new RegisterUserCommand("a@b.co", "weak", "Alice", null))
            .ShouldHaveValidationErrorFor(c => c.Password);
    }

    [Fact]
    public void RegisterUserCommandValidator_accepts_strong_password()
    {
        var v = new RegisterUserCommandValidator();
        v.TestValidate(new RegisterUserCommand("a@b.co", "Pass1word!", "Alice", null))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void LoginCommandValidator_rejects_empty_email()
    {
        new LoginCommandValidator().TestValidate(new LoginCommand("", "x", null))
            .ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Fact]
    public void RefreshTokenCommandValidator_rejects_empty_token()
    {
        new RefreshTokenCommandValidator().TestValidate(new RefreshTokenCommand("", null))
            .ShouldHaveValidationErrorFor(c => c.RefreshToken);
    }

    [Fact]
    public void UpdateProfileCommandValidator_requires_at_least_one_field()
    {
        var v = new UpdateProfileCommandValidator();
        var result = v.TestValidate(new UpdateProfileCommand(UserId.New(), null, null, null));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateProfileCommandValidator_requires_current_password_when_new_set()
    {
        var v = new UpdateProfileCommandValidator();
        v.TestValidate(new UpdateProfileCommand(UserId.New(), null, null, "NewPass1!"))
            .ShouldHaveValidationErrorFor(c => c.CurrentPassword);
    }
}

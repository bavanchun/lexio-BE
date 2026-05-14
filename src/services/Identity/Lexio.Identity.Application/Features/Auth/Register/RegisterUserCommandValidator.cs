using FluentValidation;

namespace Lexio.Identity.Application.Features.Auth.Register;

public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(c => c.Email).NotEmpty().EmailAddress().MaximumLength(255);
        RuleFor(c => c.DisplayName).NotEmpty().Length(1, 100);
        RuleFor(c => c.Password)
            .NotEmpty()
            .MinimumLength(8)
            .Matches(@"\d").WithMessage("Password must contain at least one digit.")
            .Matches(@"[^A-Za-z0-9]").WithMessage("Password must contain at least one special character.");
    }
}

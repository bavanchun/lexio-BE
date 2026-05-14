using FluentValidation;

namespace Lexio.Identity.Application.Features.Users.UpdateProfile;

public sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(c => c)
            .Must(c => !string.IsNullOrWhiteSpace(c.DisplayName) || !string.IsNullOrWhiteSpace(c.NewPassword))
            .WithName("UpdateProfile")
            .WithMessage("At least one of DisplayName or NewPassword must be supplied.");

        When(c => !string.IsNullOrWhiteSpace(c.NewPassword), () =>
        {
            RuleFor(c => c.CurrentPassword)
                .NotEmpty().WithMessage("Current password is required when changing password.");
            RuleFor(c => c.NewPassword!)
                .MinimumLength(8)
                .Matches(@"\d").WithMessage("Password must contain at least one digit.")
                .Matches(@"[^A-Za-z0-9]").WithMessage("Password must contain at least one special character.");
        });

        When(c => !string.IsNullOrWhiteSpace(c.DisplayName), () =>
        {
            RuleFor(c => c.DisplayName!).Length(1, 100);
        });
    }
}

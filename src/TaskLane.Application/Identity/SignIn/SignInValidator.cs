using FluentValidation;

namespace TaskLane.Application.Identity.SignIn;

/// <summary>
/// Only checks presence: registration password rules are not applied, so a non-matching password
/// simply produces the uniform sign-in failure (spec FR-007).
/// </summary>
public sealed class SignInValidator : AbstractValidator<SignInCommand>
{
    public SignInValidator()
    {
        RuleFor(command => command.Email)
            .Must(email => !string.IsNullOrWhiteSpace(email))
            .WithMessage("Email is required.");

        RuleFor(command => command.Password)
            .Must(password => !string.IsNullOrEmpty(password))
            .WithMessage("Password is required.");
    }
}

using FluentValidation;

using TaskLane.Application.Identity.Validation;
using TaskLane.Domain.Identity;

namespace TaskLane.Application.Identity.Register;

/// <summary>
/// Registration input rules (spec FR-003). Lengths are measured after trimming, like the stored values.
/// </summary>
public sealed class RegisterUserValidator : AbstractValidator<RegisterUserCommand>
{
    public const int PasswordMinLength = 8;
    public const int PasswordMaxLength = 128;

    public RegisterUserValidator()
    {
        RuleFor(command => command.DisplayName)
            .Cascade(CascadeMode.Stop)
            .Must(displayName => !string.IsNullOrWhiteSpace(displayName))
            .WithMessage("Display name is required.")
            .Must(displayName => displayName.Trim().Length <= User.DisplayNameMaxLength)
            .WithMessage($"Display name must be at most {User.DisplayNameMaxLength} characters.");

        RuleFor(command => command.Email)
            .Cascade(CascadeMode.Stop)
            .Must(email => !string.IsNullOrWhiteSpace(email))
            .WithMessage("Email is required.")
            .Must(email => email.Trim().Length <= User.EmailMaxLength)
            .WithMessage($"Email must be at most {User.EmailMaxLength} characters.")
            .Must(EmailFormat.IsValid)
            .WithMessage("Enter a valid email address.");

        RuleFor(command => command.Password)
            .Cascade(CascadeMode.Stop)
            .Must(password => password is { Length: >= PasswordMinLength and <= PasswordMaxLength })
            .WithMessage($"Password must be between {PasswordMinLength} and {PasswordMaxLength} characters.")
            .Must(password => !string.IsNullOrWhiteSpace(password))
            .WithMessage("Password cannot be only spaces.");
    }
}

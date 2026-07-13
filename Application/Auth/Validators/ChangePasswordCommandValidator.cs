using Application.Auth.Commands;
using Application.Common.Validation;
using FluentValidation;

namespace Application.Auth.Validators
{
    public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
    {
        public ChangePasswordCommandValidator()
        {
            RuleFor(command => command.CurrentPassword)
                .NotEmpty();

            RuleFor(command => command.NewPassword)
                .NotEmpty()
                .MinimumLength(8)
                .MaximumLength(128);

            RuleFor(command => command.NewPassword)
                .Must(password => PasswordRules.UppercasePattern.IsMatch(password))
                    .WithMessage("Password must contain at least one uppercase letter.")
                .Must(password => PasswordRules.LowercasePattern.IsMatch(password))
                    .WithMessage("Password must contain at least one lowercase letter.")
                .Must(password => PasswordRules.DigitPattern.IsMatch(password))
                    .WithMessage("Password must contain at least one number.")
                .Must(password => PasswordRules.SpecialCharacterPattern.IsMatch(password))
                    .WithMessage("Password must contain at least one special character.")
                .Must(password => !PasswordRules.CommonPasswords.Contains(password))
                    .WithMessage("Password is too common. Choose a stronger password.")
                .NotEqual(command => command.CurrentPassword)
                    .WithMessage("New password must be different from your current password.")
                .When(command => !string.IsNullOrEmpty(command.NewPassword));
        }
    }
}

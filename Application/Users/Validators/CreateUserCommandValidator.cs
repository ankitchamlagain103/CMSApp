using Application.Common.Validation;
using Application.Users.Commands;
using FluentValidation;

namespace Application.Users.Validators
{
    public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
    {
        public CreateUserCommandValidator()
        {
            RuleFor(command => command.UserName)
                .NotEmpty()
                .MaximumLength(256);

            RuleFor(command => command.Email)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(320);

            RuleFor(command => command.Password)
                .NotEmpty()
                .MinimumLength(8)
                .MaximumLength(128);

            RuleFor(command => command.Password)
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
                .NotEqual(command => command.Email, StringComparer.OrdinalIgnoreCase)
                    .WithMessage("Password must not match your email.")
                .NotEqual(command => command.UserName, StringComparer.OrdinalIgnoreCase)
                    .WithMessage("Password must not match your username.")
                .When(command => !string.IsNullOrWhiteSpace(command.Password));

            RuleFor(command => command.FirstName)
                .NotEmpty()
                .MaximumLength(256);

            RuleFor(command => command.FirstName)
                .Must(name => !UserValidationRules.NoHtmlPattern.IsMatch(name))
                    .WithMessage("First name must not contain '<' or '>'.")
                .When(command => !string.IsNullOrWhiteSpace(command.FirstName));

            RuleFor(command => command.MiddleName)
                .Must(name => !UserValidationRules.NoHtmlPattern.IsMatch(name))
                    .WithMessage("Middle name must not contain '<' or '>'.")
                .When(command => !string.IsNullOrWhiteSpace(command.MiddleName));

            RuleFor(command => command.LastName)
                .NotEmpty()
                .MaximumLength(256);

            RuleFor(command => command.LastName)
                .Must(name => !UserValidationRules.NoHtmlPattern.IsMatch(name))
                    .WithMessage("Last name must not contain '<' or '>'.")
                .When(command => !string.IsNullOrWhiteSpace(command.LastName));

            RuleFor(command => command.Dob)
                .Must(UserValidationRules.BeAValidAge)
                    .WithMessage("Age must be between 13 and 120 years.")
                .When(command => command.Dob.HasValue);

            RuleFor(command => command.PhoneNumber)
                .Must(phoneNumber => UserValidationRules.PhoneE164Pattern.IsMatch(phoneNumber))
                    .WithMessage("Phone number must be in E.164 format, e.g. +14155552671.")
                .When(command => !string.IsNullOrWhiteSpace(command.PhoneNumber));

            RuleFor(command => command.CountryIso3)
                .Must(countryIso3 => UserValidationRules.CountryIso3Pattern.IsMatch(countryIso3))
                    .WithMessage("Country must be a 3-letter ISO code, e.g. USA.")
                .When(command => !string.IsNullOrWhiteSpace(command.CountryIso3));

            RuleFor(command => command.IsTosAgreed)
                .Equal(true)
                    .WithMessage("Terms must be accepted.");

            RuleForEach(command => command.RoleIds)
                .NotEmpty()
                    .WithMessage("RoleIds must not contain an empty guid.");
        }
    }
}

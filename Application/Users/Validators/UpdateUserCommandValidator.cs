using Application.Users.Commands;
using FluentValidation;

namespace Application.Users.Validators
{
    public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
    {
        public UpdateUserCommandValidator()
        {
            RuleFor(command => command.FirstName)
                .NotEmpty()
                .MaximumLength(256);

            RuleFor(command => command.FirstName)
                .Must(name => !UserValidationRules.NoHtmlPattern.IsMatch(name))
                    .WithMessage("First name must not contain '<' or '>'.")
                .When(command => !string.IsNullOrEmpty(command.FirstName));

            RuleFor(command => command.LastName)
                .NotEmpty()
                .MaximumLength(256);

            RuleFor(command => command.LastName)
                .Must(name => !UserValidationRules.NoHtmlPattern.IsMatch(name))
                    .WithMessage("Last name must not contain '<' or '>'.")
                .When(command => !string.IsNullOrEmpty(command.LastName));

            RuleFor(command => command.MiddleName)
                .Must(name => !UserValidationRules.NoHtmlPattern.IsMatch(name))
                    .WithMessage("Middle name must not contain '<' or '>'.")
                .When(command => !string.IsNullOrEmpty(command.MiddleName));

            RuleFor(command => command.Dob)
                .Must(UserValidationRules.BeAValidAge)
                    .WithMessage("Age must be between 13 and 120 years.")
                .When(command => command.Dob.HasValue);

            RuleFor(command => command.PhoneNumber)
                .Must(phoneNumber => UserValidationRules.PhoneE164Pattern.IsMatch(phoneNumber))
                    .WithMessage("Phone number must be in E.164 format, e.g. +14155552671.")
                .When(command => !string.IsNullOrEmpty(command.PhoneNumber));

            RuleFor(command => command.CountryIso3)
                .Must(countryIso3 => UserValidationRules.CountryIso3Pattern.IsMatch(countryIso3))
                    .WithMessage("Country must be a 3-letter ISO code, e.g. USA.")
                .When(command => !string.IsNullOrEmpty(command.CountryIso3));

            RuleFor(command => command.UserIpAllowed)
                .NotEmpty()
                    .WithMessage("At least one allowed IP is required when IP restriction is enabled.")
                .When(command => command.IsIpRestricted);

            RuleFor(command => command.UserIpAllowed)
                .Must(UserValidationRules.BeAValidIpList)
                    .WithMessage("UserIpAllowed must be a comma-separated list of valid IPv4/IPv6 addresses.")
                .When(command => !string.IsNullOrEmpty(command.UserIpAllowed));

            RuleForEach(command => command.RoleIds)
                .NotEmpty()
                    .WithMessage("RoleIds must not contain an empty guid.");
        }
    }
}

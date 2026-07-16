using Application.Guardians.Commands;
using FluentValidation;

namespace Application.Guardians.Validators
{
    public class CreateGuardianCommandValidator : AbstractValidator<CreateGuardianCommand>
    {
        public CreateGuardianCommandValidator()
        {
            RuleFor(command => command.FirstName)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(command => command.LastName)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(command => command.Email)
                .EmailAddress()
                .MaximumLength(255)
                .When(command => !string.IsNullOrWhiteSpace(command.Email));

            RuleFor(command => command.Phone)
                .MaximumLength(20);

            RuleFor(command => command.Occupation)
                .MaximumLength(150);

            RuleFor(command => command.Address)
                .MaximumLength(500);
        }
    }
}

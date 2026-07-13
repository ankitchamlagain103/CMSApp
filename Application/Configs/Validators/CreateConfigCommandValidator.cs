using Application.Configs.Commands;
using FluentValidation;

namespace Application.Configs.Validators
{
    public class CreateConfigCommandValidator : AbstractValidator<CreateConfigCommand>
    {
        public CreateConfigCommandValidator()
        {
            RuleFor(command => command.TypeCode)
                .GreaterThan(0);

            RuleFor(command => command.Code)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(command => command.Label)
                .NotEmpty()
                .MaximumLength(256);

            RuleFor(command => command.Order)
                .GreaterThanOrEqualTo(0);

            RuleFor(command => command.AdditionalValue1)
                .MaximumLength(500);

            RuleFor(command => command.AdditionalValue2)
                .MaximumLength(500);

            RuleFor(command => command.AdditionalValue3)
                .MaximumLength(500);
        }
    }
}

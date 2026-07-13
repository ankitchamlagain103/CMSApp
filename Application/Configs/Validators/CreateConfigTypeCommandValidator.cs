using Application.Configs.Commands;
using FluentValidation;

namespace Application.Configs.Validators
{
    public class CreateConfigTypeCommandValidator : AbstractValidator<CreateConfigTypeCommand>
    {
        public CreateConfigTypeCommandValidator()
        {
            RuleFor(command => command.TypeCode)
                .GreaterThan(0);

            RuleFor(command => command.Name)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(command => command.Description)
                .MaximumLength(500);
        }
    }
}

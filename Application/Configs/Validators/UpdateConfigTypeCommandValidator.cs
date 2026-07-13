using Application.Configs.Commands;
using FluentValidation;

namespace Application.Configs.Validators
{
    public class UpdateConfigTypeCommandValidator : AbstractValidator<UpdateConfigTypeCommand>
    {
        public UpdateConfigTypeCommandValidator()
        {
            RuleFor(command => command.Name)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(command => command.Description)
                .MaximumLength(500);
        }
    }
}

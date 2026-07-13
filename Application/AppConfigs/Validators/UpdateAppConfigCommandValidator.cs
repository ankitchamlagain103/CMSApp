using Application.AppConfigs.Commands;
using FluentValidation;

namespace Application.AppConfigs.Validators
{
    public class UpdateAppConfigCommandValidator : AbstractValidator<UpdateAppConfigCommand>
    {
        public UpdateAppConfigCommandValidator()
        {
            RuleFor(command => command.ConfigParam)
                .NotEmpty()
                .MaximumLength(256);

            RuleFor(command => command.ConfigValue)
                .NotEmpty()
                .MaximumLength(555);

            RuleFor(command => command.ConfigGroup)
                .MaximumLength(256);
        }
    }
}

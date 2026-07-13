using Application.Menus.Commands;
using Domain.Constants;
using FluentValidation;

namespace Application.Menus.Validators
{
    public class UpdateMenuCommandValidator : AbstractValidator<UpdateMenuCommand>
    {
        public UpdateMenuCommandValidator()
        {
            RuleFor(command => command.Code)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(command => command.DisplayName)
                .NotEmpty()
                .MaximumLength(256);

            RuleFor(command => command.MenuType)
                .NotEmpty()
                .MaximumLength(50);

            RuleFor(command => command.MenuType)
                .Must(menuType => MenuTypes.All.Contains(menuType))
                    .WithMessage("MenuType must be one of: " + string.Join(", ", MenuTypes.All) + ".")
                .When(command => !string.IsNullOrEmpty(command.MenuType));

            RuleFor(command => command.MenuFor)
                .NotEmpty()
                .MaximumLength(50);

            RuleFor(command => command.MenuFor)
                .Must(menuFor => MenuAudience.All.Contains(menuFor))
                    .WithMessage("MenuFor must be one of: " + string.Join(", ", MenuAudience.All) + ".")
                .When(command => !string.IsNullOrEmpty(command.MenuFor));

            RuleFor(command => command.Order)
                .GreaterThanOrEqualTo(0);
        }
    }
}

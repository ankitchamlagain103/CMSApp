using Application.Roles.Commands;
using FluentValidation;

namespace Application.Roles.Validators
{
    public class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
    {
        public CreateRoleCommandValidator()
        {
            RuleFor(command => command.Name)
                .NotEmpty()
                .MaximumLength(256);

            RuleFor(command => command.Description)
                .MaximumLength(500);
        }
    }
}

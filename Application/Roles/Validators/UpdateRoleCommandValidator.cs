using Application.Roles.Commands;
using FluentValidation;

namespace Application.Roles.Validators
{
    public class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
    {
        public UpdateRoleCommandValidator()
        {
            RuleFor(command => command.Name)
                .NotEmpty()
                .MaximumLength(256);

            RuleFor(command => command.Description)
                .MaximumLength(500);
        }
    }
}

using Application.Roles.Commands;
using FluentValidation;

namespace Application.Roles.Validators
{
    public class AssignMenuToRoleCommandValidator : AbstractValidator<AssignMenuToRoleCommand>
    {
        public AssignMenuToRoleCommandValidator()
        {
            RuleFor(command => command.RoleId)
                .NotEmpty();

            RuleFor(command => command.MenuId)
                .GreaterThan(0);
        }
    }
}

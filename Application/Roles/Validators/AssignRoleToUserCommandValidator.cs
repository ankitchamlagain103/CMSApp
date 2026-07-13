using Application.Roles.Commands;
using FluentValidation;

namespace Application.Roles.Validators
{
    public class AssignRoleToUserCommandValidator : AbstractValidator<AssignRoleToUserCommand>
    {
        public AssignRoleToUserCommandValidator()
        {
            RuleFor(command => command.UserId)
                .NotEmpty();

            RuleFor(command => command.RoleId)
                .NotEmpty();
        }
    }
}

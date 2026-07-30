using Application.Employees.Commands;
using FluentValidation;

namespace Application.Employees.Validators
{
    public class RegisterEmployeeUserAccountCommandValidator : AbstractValidator<RegisterEmployeeUserAccountCommand>
    {
        public RegisterEmployeeUserAccountCommandValidator()
        {
            RuleFor(command => command.RoleIds)
                .Must(roleIds => roleIds != null && roleIds.Count > 0)
                .WithMessage("At least one role is required to create a portal account.");
        }
    }
}

using Application.Roles.Commands;
using Domain.Constants;
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

            RuleFor(command => command.UserType)
                .Must(userType => MenuAudience.All.Contains(userType))
                .When(command => !string.IsNullOrEmpty(command.UserType))
                .WithMessage("UserType must be one of: ADMIN, USER, BOTH.");
        }
    }
}

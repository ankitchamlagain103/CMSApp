using Application.Users.Commands;
using FluentValidation;

namespace Application.Users.Validators
{
    public class AssignMenuToUserCommandValidator : AbstractValidator<AssignMenuToUserCommand>
    {
        public AssignMenuToUserCommandValidator()
        {
            RuleFor(command => command.UserId)
                .NotEmpty();

            RuleFor(command => command.MenuId)
                .GreaterThan(0);
        }
    }
}

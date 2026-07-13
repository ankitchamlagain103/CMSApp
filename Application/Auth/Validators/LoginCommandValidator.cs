using Application.Auth.Commands;
using FluentValidation;

namespace Application.Auth.Validators
{
    public class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            RuleFor(command => command.UserName)
                .NotEmpty();

            RuleFor(command => command.Password)
                .NotEmpty();
        }
    }
}

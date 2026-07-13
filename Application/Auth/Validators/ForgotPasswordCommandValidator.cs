using Application.Auth.Commands;
using FluentValidation;

namespace Application.Auth.Validators
{
    public class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
    {
        public ForgotPasswordCommandValidator()
        {
            RuleFor(command => command.Email)
                .NotEmpty()
                .EmailAddress();
        }
    }
}

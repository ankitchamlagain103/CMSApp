using Application.Auth.Commands;
using FluentValidation;

namespace Application.Auth.Validators
{
    public class ResendVerificationEmailCommandValidator : AbstractValidator<ResendVerificationEmailCommand>
    {
        public ResendVerificationEmailCommandValidator()
        {
            RuleFor(command => command.Email)
                .NotEmpty()
                .EmailAddress();
        }
    }
}

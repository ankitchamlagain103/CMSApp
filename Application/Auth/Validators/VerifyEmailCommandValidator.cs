using Application.Auth.Commands;
using FluentValidation;

namespace Application.Auth.Validators
{
    public class VerifyEmailCommandValidator : AbstractValidator<VerifyEmailCommand>
    {
        public VerifyEmailCommandValidator()
        {
            RuleFor(command => command.UserId)
                .NotEmpty();

            RuleFor(command => command.Token)
                .NotEmpty();
        }
    }
}

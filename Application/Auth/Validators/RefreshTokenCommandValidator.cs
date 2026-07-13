using Application.Auth.Commands;
using FluentValidation;

namespace Application.Auth.Validators
{
    public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
    {
        public RefreshTokenCommandValidator()
        {
            RuleFor(command => command.RefreshToken)
                .NotEmpty();
        }
    }
}

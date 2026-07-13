using Application.Auth.Commands;
using FluentValidation;

namespace Application.Auth.Validators
{
    public class GoogleLoginCommandValidator : AbstractValidator<GoogleLoginCommand>
    {
        public GoogleLoginCommandValidator()
        {
            RuleFor(command => command.IdToken)
                .NotEmpty();
        }
    }
}

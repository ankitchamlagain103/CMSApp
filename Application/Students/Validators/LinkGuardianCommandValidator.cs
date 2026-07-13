using Application.Students.Commands;
using FluentValidation;

namespace Application.Students.Validators
{
    public class LinkGuardianCommandValidator : AbstractValidator<LinkGuardianCommand>
    {
        public LinkGuardianCommandValidator()
        {
            RuleFor(command => command.GuardianId)
                .NotEmpty();

            RuleFor(command => command.RelationshipCode)
                .NotEmpty()
                .MaximumLength(100);
        }
    }
}

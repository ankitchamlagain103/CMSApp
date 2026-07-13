using Application.AcademicClasses.Commands;
using FluentValidation;

namespace Application.AcademicClasses.Validators
{
    public class CreateClassSectionCommandValidator : AbstractValidator<CreateClassSectionCommand>
    {
        public CreateClassSectionCommandValidator()
        {
            RuleFor(command => command.SectionCode)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(command => command.Capacity)
                .GreaterThanOrEqualTo(0)
                    .WithMessage("Capacity must be 0 (unlimited) or a positive number.");
        }
    }
}

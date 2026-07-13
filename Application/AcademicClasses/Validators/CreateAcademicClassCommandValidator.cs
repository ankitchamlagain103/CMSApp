using Application.AcademicClasses.Commands;
using FluentValidation;

namespace Application.AcademicClasses.Validators
{
    public class CreateAcademicClassCommandValidator : AbstractValidator<CreateAcademicClassCommand>
    {
        public CreateAcademicClassCommandValidator()
        {
            RuleFor(command => command.AcademicYearId)
                .NotEmpty();

            RuleFor(command => command.GradeCode)
                .NotEmpty()
                .MaximumLength(100);

            RuleForEach(command => command.Sections)
                .SetValidator(new CreateClassSectionCommandValidator());
        }
    }
}

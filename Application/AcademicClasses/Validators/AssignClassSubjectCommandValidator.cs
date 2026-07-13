using Application.AcademicClasses.Commands;
using FluentValidation;

namespace Application.AcademicClasses.Validators
{
    public class AssignClassSubjectCommandValidator : AbstractValidator<AssignClassSubjectCommand>
    {
        public AssignClassSubjectCommandValidator()
        {
            RuleFor(command => command.SubjectCode)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(command => command.DisplayOrder)
                .GreaterThanOrEqualTo(0);
        }
    }
}

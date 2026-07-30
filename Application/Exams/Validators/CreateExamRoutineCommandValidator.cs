using Application.Exams.Commands;
using FluentValidation;

namespace Application.Exams.Validators
{
    public class CreateExamRoutineCommandValidator : AbstractValidator<CreateExamRoutineCommand>
    {
        public CreateExamRoutineCommandValidator()
        {
            RuleFor(command => command.ExamTermId)
                .NotEmpty();

            RuleFor(command => command.AcademicClassId)
                .NotEmpty();

            RuleFor(command => command.Items)
                .NotEmpty();

            RuleForEach(command => command.Items)
                .SetValidator(new ExamRoutineItemInputValidator());
        }
    }
}

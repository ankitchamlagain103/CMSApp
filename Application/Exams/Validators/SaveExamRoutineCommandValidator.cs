using Application.Exams.Commands;
using FluentValidation;

namespace Application.Exams.Validators
{
    public class SaveExamRoutineCommandValidator : AbstractValidator<SaveExamRoutineCommand>
    {
        public SaveExamRoutineCommandValidator()
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

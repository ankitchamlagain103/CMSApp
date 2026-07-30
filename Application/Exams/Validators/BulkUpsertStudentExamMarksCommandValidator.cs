using Application.Exams.Commands;
using FluentValidation;

namespace Application.Exams.Validators
{
    public class BulkUpsertStudentExamMarksCommandValidator : AbstractValidator<BulkUpsertStudentExamMarksCommand>
    {
        public BulkUpsertStudentExamMarksCommandValidator()
        {
            RuleFor(command => command.ExamId)
                .NotEmpty();

            RuleFor(command => command.Marks)
                .NotEmpty();

            RuleForEach(command => command.Marks)
                .SetValidator(new StudentExamMarkLineInputValidator());
        }
    }
}

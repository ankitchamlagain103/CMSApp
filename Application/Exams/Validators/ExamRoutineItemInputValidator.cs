using Application.Exams.Commands;
using FluentValidation;

namespace Application.Exams.Validators
{
    public class ExamRoutineItemInputValidator : AbstractValidator<ExamRoutineItemInput>
    {
        public ExamRoutineItemInputValidator()
        {
            RuleFor(item => item.ClassSubjectId)
                .NotEmpty();

            RuleFor(item => item.Remarks)
                .MaximumLength(500);

            RuleFor(item => item)
                .Must(item => item.EndTime > item.StartTime)
                    .WithMessage("EndTime must be after StartTime.");
        }
    }
}

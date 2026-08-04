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
                .Must(item => item.TimePeriodId.HasValue || (item.StartTime.HasValue && item.EndTime.HasValue))
                    .WithMessage("Either TimePeriodId or both StartTime and EndTime must be provided.")
                .Must(item => !item.StartTime.HasValue || !item.EndTime.HasValue || item.EndTime.Value > item.StartTime.Value)
                    .WithMessage("EndTime must be after StartTime.");
        }
    }
}

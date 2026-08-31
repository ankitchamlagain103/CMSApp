using Application.Exams.Commands;
using FluentValidation;

namespace Application.Exams.Validators
{
    public class UpdateExamCommandValidator : AbstractValidator<UpdateExamCommand>
    {
        public UpdateExamCommandValidator()
        {
            RuleFor(command => command.Remarks)
                .MaximumLength(500);

            RuleFor(command => command)
                .Must(command => command.TimePeriodId.HasValue || (command.StartTime.HasValue && command.EndTime.HasValue))
                    .WithMessage("Either TimePeriodId or both StartTime and EndTime must be provided.")
                .Must(command => !command.StartTime.HasValue || !command.EndTime.HasValue || command.EndTime.Value > command.StartTime.Value)
                    .WithMessage("EndTime must be after StartTime.");
        }
    }
}

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
                .Must(command => command.EndTime > command.StartTime)
                    .WithMessage("EndTime must be after StartTime.");
        }
    }
}

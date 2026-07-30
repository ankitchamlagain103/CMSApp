using Application.Exams.Commands;
using FluentValidation;

namespace Application.Exams.Validators
{
    public class CreateExamCommandValidator : AbstractValidator<CreateExamCommand>
    {
        public CreateExamCommandValidator()
        {
            RuleFor(command => command.ExamTermId)
                .NotEmpty();

            RuleFor(command => command.ClassSubjectId)
                .NotEmpty();

            RuleFor(command => command.Remarks)
                .MaximumLength(500);

            RuleFor(command => command)
                .Must(command => command.EndTime > command.StartTime)
                    .WithMessage("EndTime must be after StartTime.");
        }
    }
}

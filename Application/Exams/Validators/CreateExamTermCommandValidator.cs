using Application.Exams.Commands;
using FluentValidation;

namespace Application.Exams.Validators
{
    public class CreateExamTermCommandValidator : AbstractValidator<CreateExamTermCommand>
    {
        public CreateExamTermCommandValidator()
        {
            RuleFor(command => command.AcademicYearId)
                .NotEmpty();

            RuleFor(command => command.Code)
                .NotEmpty()
                .MaximumLength(20);

            RuleFor(command => command.Name)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(command => command.Sequence)
                .GreaterThanOrEqualTo(0);

            RuleFor(command => command.Status)
                .IsInEnum();

            RuleFor(command => command)
                .Must(command => command.EndDate >= command.StartDate)
                    .WithMessage("EndDate cannot be before StartDate.");
        }
    }
}

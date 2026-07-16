using Application.AcademicClasses.Commands;
using FluentValidation;

namespace Application.AcademicClasses.Validators
{
    public class UpdateClassSubjectCommandValidator : AbstractValidator<UpdateClassSubjectCommand>
    {
        public UpdateClassSubjectCommandValidator()
        {
            RuleFor(command => command.DisplayOrder)
                .GreaterThanOrEqualTo(0);

            RuleFor(command => command.CreditHours)
                .GreaterThan(0)
                .When(command => command.CreditHours.HasValue);

            RuleFor(command => command.FullMarks)
                .GreaterThan(0)
                .When(command => command.FullMarks.HasValue);

            RuleFor(command => command)
                .Must(command => !command.PassMarks.HasValue || !command.FullMarks.HasValue || command.PassMarks.Value <= command.FullMarks.Value)
                    .WithMessage("PassMarks cannot exceed FullMarks.")
                .Must(command => !command.TheoryMarks.HasValue || !command.PracticalMarks.HasValue || !command.FullMarks.HasValue || command.TheoryMarks.Value + command.PracticalMarks.Value == command.FullMarks.Value)
                    .WithMessage("TheoryMarks and PracticalMarks must add up to FullMarks when both are supplied.");
        }
    }
}

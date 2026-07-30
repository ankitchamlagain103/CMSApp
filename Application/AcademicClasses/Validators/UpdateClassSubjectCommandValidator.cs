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
                    .WithMessage("TheoryMarks and PracticalMarks must add up to FullMarks when both are supplied.")
                .Must(command => command.HasTheory || command.HasPractical)
                    .WithMessage("At least one of HasTheory/HasPractical must be enabled.")
                .Must(command => command.HasTheory || (!command.TheoryMarks.HasValue && !command.TheoryPassMarks.HasValue))
                    .WithMessage("TheoryMarks/TheoryPassMarks cannot be set when HasTheory is false.")
                .Must(command => command.HasPractical || (!command.PracticalMarks.HasValue && !command.PracticalPassMarks.HasValue))
                    .WithMessage("PracticalMarks/PracticalPassMarks cannot be set when HasPractical is false.")
                .Must(command => !command.TheoryPassMarks.HasValue || !command.TheoryMarks.HasValue || command.TheoryPassMarks.Value <= command.TheoryMarks.Value)
                    .WithMessage("TheoryPassMarks cannot exceed TheoryMarks.")
                .Must(command => !command.PracticalPassMarks.HasValue || !command.PracticalMarks.HasValue || command.PracticalPassMarks.Value <= command.PracticalMarks.Value)
                    .WithMessage("PracticalPassMarks cannot exceed PracticalMarks.");
        }
    }
}

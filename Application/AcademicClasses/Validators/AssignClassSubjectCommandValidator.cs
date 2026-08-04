using Application.AcademicClasses.Commands;
using FluentValidation;

namespace Application.AcademicClasses.Validators
{
    public class AssignClassSubjectCommandValidator : AbstractValidator<AssignClassSubjectCommand>
    {
        public AssignClassSubjectCommandValidator()
        {
            RuleFor(command => command.SubjectCode)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(command => command.DisplayOrder)
                .GreaterThanOrEqualTo(0);

            RuleFor(command => command.CreditHours)
                .GreaterThan(0)
                .When(command => command.CreditHours.HasValue);

            RuleFor(command => command.TheoryMarks)
                .GreaterThan(0)
                .When(command => command.TheoryMarks.HasValue);

            RuleFor(command => command.PracticalMarks)
                .GreaterThan(0)
                .When(command => command.PracticalMarks.HasValue);

            // PassMarks <= FullMarks is no longer checked directly here -- since FullMarks/
            // PassMarks are now computed as the sum of these same Theory/Practical pairs
            // (AcademicClassService.ResolveCompositeMarks), TheoryPassMarks <= TheoryMarks and
            // PracticalPassMarks <= PracticalMarks together already guarantee it (sum of two
            // component inequalities preserves the total inequality).
            RuleFor(command => command)
                .Must(command => command.HasTheory || command.HasPractical)
                    .WithMessage("At least one of HasTheory/HasPractical must be enabled.")
                .Must(command => command.HasTheory || (!command.TheoryMarks.HasValue && !command.TheoryPassMarks.HasValue))
                    .WithMessage("TheoryMarks/TheoryPassMarks cannot be set when HasTheory is false.")
                .Must(command => command.HasPractical || (!command.PracticalMarks.HasValue && !command.PracticalPassMarks.HasValue))
                    .WithMessage("PracticalMarks/PracticalPassMarks cannot be set when HasPractical is false.")
                .Must(command => !command.TheoryPassMarks.HasValue || !command.TheoryMarks.HasValue || command.TheoryPassMarks.Value <= command.TheoryMarks.Value)
                    .WithMessage("TheoryPassMarks cannot exceed TheoryMarks.")
                .Must(command => !command.PracticalPassMarks.HasValue || !command.PracticalMarks.HasValue || command.PracticalPassMarks.Value <= command.PracticalMarks.Value)
                    .WithMessage("PracticalPassMarks cannot exceed PracticalMarks.")
                .Must(command => !command.HasTheory || !command.HasPractical || command.TheoryMarks.HasValue == command.PracticalMarks.HasValue)
                    .WithMessage("When both HasTheory and HasPractical are enabled, TheoryMarks and PracticalMarks must be provided together -- both set (to compute Total Full Marks) or both left unset until grading is configured.");
        }
    }
}

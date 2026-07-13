using Application.AcademicClasses.Commands;
using FluentValidation;

namespace Application.AcademicClasses.Validators
{
    public class UpdateClassSectionCommandValidator : AbstractValidator<UpdateClassSectionCommand>
    {
        public UpdateClassSectionCommandValidator()
        {
            RuleFor(command => command.Capacity)
                .GreaterThanOrEqualTo(0)
                    .WithMessage("Capacity must be 0 (unlimited) or a positive number.");

            RuleFor(command => command.Status)
                .IsInEnum();
        }
    }
}

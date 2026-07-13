using Application.AcademicYears.Commands;
using FluentValidation;

namespace Application.AcademicYears.Validators
{
    public class UpdateAcademicYearCommandValidator : AbstractValidator<UpdateAcademicYearCommand>
    {
        public UpdateAcademicYearCommandValidator()
        {
            RuleFor(command => command.Name)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(command => command.EndDate)
                .GreaterThan(command => command.StartDate)
                    .WithMessage("EndDate must be after StartDate.");

            RuleFor(command => command.Status)
                .IsInEnum();
        }
    }
}

using Application.AcademicYears.Commands;
using FluentValidation;

namespace Application.AcademicYears.Validators
{
    public class CreateAcademicYearCommandValidator : AbstractValidator<CreateAcademicYearCommand>
    {
        public CreateAcademicYearCommandValidator()
        {
            RuleFor(command => command.Code)
                .NotEmpty()
                .MaximumLength(20);

            RuleFor(command => command.Name)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(command => command.EndDate)
                .GreaterThan(command => command.StartDate)
                    .WithMessage("EndDate must be after StartDate.");
        }
    }
}

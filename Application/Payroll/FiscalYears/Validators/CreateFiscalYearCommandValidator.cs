using Application.Payroll.FiscalYears.Commands;
using FluentValidation;

namespace Application.Payroll.FiscalYears.Validators
{
    public class CreateFiscalYearCommandValidator : AbstractValidator<CreateFiscalYearCommand>
    {
        public CreateFiscalYearCommandValidator()
        {
            RuleFor(command => command.Code)
                .NotEmpty()
                .MaximumLength(20);

            RuleFor(command => command.Name)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(command => command.EndDate)
                .GreaterThan(command => command.StartDate);

            RuleFor(command => command.RetirementExemptionCapAmount)
                .GreaterThanOrEqualTo(0);
        }
    }
}

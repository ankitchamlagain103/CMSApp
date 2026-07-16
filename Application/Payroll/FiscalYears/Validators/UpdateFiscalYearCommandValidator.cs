using Application.Payroll.FiscalYears.Commands;
using FluentValidation;

namespace Application.Payroll.FiscalYears.Validators
{
    public class UpdateFiscalYearCommandValidator : AbstractValidator<UpdateFiscalYearCommand>
    {
        public UpdateFiscalYearCommandValidator()
        {
            RuleFor(command => command.Name)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(command => command.EndDate)
                .GreaterThan(command => command.StartDate);
        }
    }
}

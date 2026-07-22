using Application.PayrollRuns.Commands;
using FluentValidation;

namespace Application.PayrollRuns.Validators
{
    public class CreatePayrollRunCommandValidator : AbstractValidator<CreatePayrollRunCommand>
    {
        public CreatePayrollRunCommandValidator()
        {
            RuleFor(command => command.FiscalYearId)
                .NotEmpty();

            RuleFor(command => command.MonthIndex)
                .InclusiveBetween(1, 12);

            RuleFor(command => command.Remarks)
                .MaximumLength(500);
        }
    }
}

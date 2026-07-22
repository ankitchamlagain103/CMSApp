using Application.PayrollRuns.Commands;
using Domain.Enums;
using FluentValidation;

namespace Application.PayrollRuns.Validators
{
    public class SalarySlipLineInputValidator : AbstractValidator<SalarySlipLineInput>
    {
        public SalarySlipLineInputValidator()
        {
            RuleFor(line => line.LineType)
                .Must(lineType => lineType == SalaryLineType.Earning || lineType == SalaryLineType.Deduction)
                .WithMessage("Manual lines can only be Earning or Deduction -- Tax and LoanEmi lines are machine-generated.");

            RuleFor(line => line.Description)
                .NotEmpty()
                .MaximumLength(300);

            RuleFor(line => line.Amount)
                .GreaterThan(0);

            RuleFor(line => line.ComponentCode)
                .MaximumLength(100);
        }
    }
}

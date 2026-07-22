using Application.Payroll.SalaryCalculations.Commands;
using FluentValidation;

namespace Application.Payroll.SalaryCalculations.Validators
{
    public class CalculateSalaryStructureCommandValidator : AbstractValidator<CalculateSalaryStructureCommand>
    {
        public CalculateSalaryStructureCommandValidator()
        {
            RuleFor(x => x.Basis)
                .IsInEnum()
                .WithMessage("Basis must be NetPayment (1), GrossPayment (2) or Ctc (3).");

            RuleFor(x => x.Amount)
                .GreaterThan(0m)
                .WithMessage("Amount must be greater than zero.");

            RuleFor(x => x.AssessmentType)
                .IsInEnum()
                .WithMessage("AssessmentType must be Individual (1) or Couple (2).");

            RuleFor(x => x.BasicSalaryAmount)
                .GreaterThan(0m)
                .When(x => x.BasicSalaryAmount.HasValue)
                .WithMessage("BasicSalaryAmount must be greater than zero when supplied.");

            RuleFor(x => x.BasicPercentOfGross)
                .InclusiveBetween(1m, 100m)
                .When(x => x.BasicPercentOfGross.HasValue)
                .WithMessage("BasicPercentOfGross must be between 1 and 100 when supplied.");

            RuleFor(x => x.AnnualBonusAmount)
                .GreaterThanOrEqualTo(0m)
                .When(x => x.AnnualBonusAmount.HasValue)
                .WithMessage("AnnualBonusAmount cannot be negative.");

            RuleFor(x => x.MonthlyCitAmount)
                .GreaterThanOrEqualTo(0m)
                .When(x => x.MonthlyCitAmount.HasValue)
                .WithMessage("MonthlyCitAmount cannot be negative.");

            RuleFor(x => x.AnnualLifeInsurancePremium)
                .GreaterThanOrEqualTo(0m)
                .When(x => x.AnnualLifeInsurancePremium.HasValue)
                .WithMessage("AnnualLifeInsurancePremium cannot be negative.");

            RuleFor(x => x.AnnualHealthInsurancePremium)
                .GreaterThanOrEqualTo(0m)
                .When(x => x.AnnualHealthInsurancePremium.HasValue)
                .WithMessage("AnnualHealthInsurancePremium cannot be negative.");
        }
    }
}

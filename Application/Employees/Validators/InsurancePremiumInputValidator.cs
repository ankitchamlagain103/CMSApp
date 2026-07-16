using Application.Employees.Commands;
using FluentValidation;

namespace Application.Employees.Validators
{
    public class InsurancePremiumInputValidator : AbstractValidator<InsurancePremiumInput>
    {
        public InsurancePremiumInputValidator()
        {
            RuleFor(p => p.InsuranceTypeCode).NotEmpty().MaximumLength(100);
            RuleFor(p => p.AnnualPremiumAmount).GreaterThanOrEqualTo(0);
        }
    }
}

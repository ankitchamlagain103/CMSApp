using Application.Employees.Commands;
using Domain.Enums;
using FluentValidation;

namespace Application.Employees.Validators
{
    public class AddEmployeeSalaryCommandValidator : AbstractValidator<AddEmployeeSalaryCommand>
    {
        public AddEmployeeSalaryCommandValidator()
        {
            RuleForEach(command => command.Components).ChildRules(component =>
            {
                component.RuleFor(c => c.ComponentCode).NotEmpty().MaximumLength(100);
                component.RuleFor(c => c.Value).GreaterThanOrEqualTo(0);
                component.RuleFor(c => c.Value)
                    .LessThanOrEqualTo(100)
                        .WithMessage("A percentage component cannot exceed 100.")
                    .When(c => c.ValueType == AwardValueType.Percentage);
            });

            RuleForEach(command => command.Deductions).ChildRules(deduction =>
            {
                deduction.RuleFor(d => d.DeductionCode).NotEmpty().MaximumLength(100);
                deduction.RuleFor(d => d.Value).GreaterThanOrEqualTo(0);
                deduction.RuleFor(d => d.Value)
                    .LessThanOrEqualTo(100)
                        .WithMessage("A percentage deduction cannot exceed 100.")
                    .When(d => d.ValueType == AwardValueType.Percentage);
            });

            RuleForEach(command => command.InsurancePremiums).ChildRules(premium =>
            {
                premium.RuleFor(p => p.InsuranceTypeCode).NotEmpty().MaximumLength(100);
                premium.RuleFor(p => p.AnnualPremiumAmount).GreaterThanOrEqualTo(0);
            });
        }
    }
}

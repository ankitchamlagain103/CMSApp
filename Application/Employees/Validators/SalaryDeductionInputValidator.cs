using Application.Employees.Commands;
using Domain.Enums;
using FluentValidation;

namespace Application.Employees.Validators
{
    public class SalaryDeductionInputValidator : AbstractValidator<SalaryDeductionInput>
    {
        public SalaryDeductionInputValidator()
        {
            RuleFor(d => d.DeductionCode).NotEmpty().MaximumLength(100);
            RuleFor(d => d.Value).GreaterThanOrEqualTo(0);
            RuleFor(d => d.Value)
                .LessThanOrEqualTo(100)
                    .WithMessage("A percentage deduction cannot exceed 100.")
                .When(d => d.ValueType == AwardValueType.Percentage);
        }
    }
}

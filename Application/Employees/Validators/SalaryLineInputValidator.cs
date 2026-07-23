using Application.Employees.Commands;
using Domain.Enums;
using FluentValidation;

namespace Application.Employees.Validators
{
    public class SalaryLineInputValidator : AbstractValidator<SalaryLineInput>
    {
        public SalaryLineInputValidator()
        {
            RuleFor(c => c.Code).NotEmpty().MaximumLength(100);
            RuleFor(c => c.Value).GreaterThanOrEqualTo(0);
            RuleFor(c => c.Value)
                .LessThanOrEqualTo(100)
                    .WithMessage("A percentage line cannot exceed 100.")
                .When(c => c.ValueType == AwardValueType.Percentage);
        }
    }
}

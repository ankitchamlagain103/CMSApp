using Application.Employees.Commands;
using Domain.Enums;
using FluentValidation;

namespace Application.Employees.Validators
{
    // Used by the standalone "add one component to an existing revision" endpoint.
    public class SalaryComponentInputValidator : AbstractValidator<SalaryComponentInput>
    {
        public SalaryComponentInputValidator()
        {
            RuleFor(c => c.ComponentCode).NotEmpty().MaximumLength(100);
            RuleFor(c => c.Value).GreaterThanOrEqualTo(0);
            RuleFor(c => c.Value)
                .LessThanOrEqualTo(100)
                    .WithMessage("A percentage component cannot exceed 100.")
                .When(c => c.ValueType == AwardValueType.Percentage);
        }
    }
}

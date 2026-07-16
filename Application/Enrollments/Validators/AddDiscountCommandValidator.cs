using Application.Enrollments.Commands;
using Domain.Enums;
using FluentValidation;

namespace Application.Enrollments.Validators
{
    public class AddDiscountCommandValidator : AbstractValidator<AddDiscountCommand>
    {
        public AddDiscountCommandValidator()
        {
            RuleFor(command => command.DiscountTypeCode)
                .NotEmpty()
                .MaximumLength(100);

            // Both or neither -- the service resolves an omitted pair from the DiscountType's
            // configured default; a half-supplied pair is always a caller mistake.
            RuleFor(command => command)
                .Must(command => command.ValueType.HasValue == command.Value.HasValue)
                    .WithMessage("Supply both ValueType and Value together, or omit both to use the discount type's configured default rate.");

            RuleFor(command => command.Value)
                .GreaterThan(0)
                .When(command => command.Value.HasValue);

            RuleFor(command => command.Value)
                .LessThanOrEqualTo(100)
                    .WithMessage("A percentage discount cannot exceed 100.")
                .When(command => command.Value.HasValue && command.ValueType == AwardValueType.Percentage);

            RuleFor(command => command.Remarks)
                .MaximumLength(500);
        }
    }
}

using Application.Employees.Commands;
using Domain.Constants;
using Domain.Enums;
using FluentValidation;

namespace Application.Employees.Validators
{
    public class CreateSalaryAdjustmentCommandValidator : AbstractValidator<CreateSalaryAdjustmentCommand>
    {
        public CreateSalaryAdjustmentCommandValidator()
        {
            RuleFor(command => command.FiscalYearId)
                .NotEmpty();

            RuleFor(command => command.MonthIndex)
                .InclusiveBetween(1, 12);

            RuleFor(command => command.AdjustmentTypeCode)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(command => command.Direction)
                .IsInEnum();

            RuleFor(command => command.ValueType)
                .IsInEnum();

            RuleFor(command => command.Value)
                .GreaterThan(0)
                .When(command => !IsUnpaidLeave(command.AdjustmentTypeCode))
                .WithMessage("Value must be greater than 0 (for UNPAID_LEAVE, Quantity carries the day count instead).");

            RuleFor(command => command.Value)
                .LessThanOrEqualTo(100)
                .When(command => command.ValueType == AwardValueType.Percentage)
                .WithMessage("A percentage value cannot exceed 100.");

            RuleFor(command => command.Quantity)
                .NotNull()
                .GreaterThan(0)
                .When(command => IsUnpaidLeave(command.AdjustmentTypeCode))
                .WithMessage("Quantity (the unpaid-leave day count) is required for an UNPAID_LEAVE adjustment.");

            RuleFor(command => command.Quantity)
                .GreaterThan(0)
                .When(command => command.Quantity.HasValue);

            RuleFor(command => command.Direction)
                .Equal(AdjustmentDirection.Decrease)
                .When(command => IsUnpaidLeave(command.AdjustmentTypeCode))
                .WithMessage("An UNPAID_LEAVE adjustment is always a deduction (Direction = Decrease).");

            RuleFor(command => command.Remarks)
                .MaximumLength(500);
        }

        private static bool IsUnpaidLeave(string adjustmentTypeCode)
        {
            return adjustmentTypeCode != null && adjustmentTypeCode.Trim() == SalaryAdjustmentTypeCodes.UnpaidLeave;
        }
    }
}

using Application.FeeInvoices.Commands;
using Domain.Enums;
using FluentValidation;

namespace Application.FeeInvoices.Validators
{
    public class CreateFeeAdjustmentCommandValidator : AbstractValidator<CreateFeeAdjustmentCommand>
    {
        public CreateFeeAdjustmentCommandValidator()
        {
            RuleFor(command => command.EnrollmentId)
                .NotEmpty();

            RuleFor(command => command.BillingYear)
                .InclusiveBetween(2000, 2100);

            RuleFor(command => command.BillingMonth)
                .InclusiveBetween(1, 12);

            RuleFor(command => command.AdjustmentTypeCode)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(command => command.FeeCategoryCode)
                .MaximumLength(100);

            RuleFor(command => command.Direction)
                .IsInEnum();

            RuleFor(command => command.ValueType)
                .IsInEnum();

            RuleFor(command => command.Value)
                .GreaterThan(0);

            RuleFor(command => command.Value)
                .LessThanOrEqualTo(100)
                .When(command => command.ValueType == AwardValueType.Percentage)
                .WithMessage("A percentage value cannot exceed 100.");

            RuleFor(command => command.Remarks)
                .MaximumLength(500);
        }
    }
}

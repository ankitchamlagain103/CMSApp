using Application.FeeRules.Commands;
using Domain.Enums;
using FluentValidation;

namespace Application.FeeRules.Validators
{
    public class CreateFeeRuleCommandValidator : AbstractValidator<CreateFeeRuleCommand>
    {
        public CreateFeeRuleCommandValidator()
        {
            RuleFor(command => command.Code)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(command => command.Name)
                .NotEmpty()
                .MaximumLength(200);

            RuleFor(command => command.RuleType)
                .IsInEnum();

            RuleFor(command => command.ValueType)
                .IsInEnum();

            RuleFor(command => command.Value)
                .GreaterThan(0);

            RuleFor(command => command.Value)
                .LessThanOrEqualTo(100)
                .When(command => command.ValueType == AwardValueType.Percentage)
                .WithMessage("A percentage value cannot exceed 100.");

            RuleFor(command => command.MinMonthsTogether)
                .NotNull()
                .GreaterThanOrEqualTo(2)
                .When(command => command.RuleType == FeeRuleType.AdvanceMonthsDiscount)
                .WithMessage("MinMonthsTogether (at least 2) is required for an advance-months discount rule.");

            RuleFor(command => command.DaysBeforeDueDate)
                .NotNull()
                .GreaterThanOrEqualTo(0)
                .When(command => command.RuleType == FeeRuleType.EarlyPaymentDiscount)
                .WithMessage("DaysBeforeDueDate (0 or more) is required for an early-payment discount rule.");

            RuleFor(command => command.EffectiveFrom)
                .NotEmpty();

            RuleFor(command => command.EffectiveTo)
                .GreaterThan(command => command.EffectiveFrom)
                .When(command => command.EffectiveTo.HasValue)
                .WithMessage("EffectiveTo must be after EffectiveFrom.");

            RuleFor(command => command.Priority)
                .GreaterThanOrEqualTo(0);

            RuleFor(command => command.FeeCategoryCode)
                .MaximumLength(100);
        }
    }
}

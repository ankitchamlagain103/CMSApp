using Application.FeeRules.Commands;
using Domain.Enums;
using FluentValidation;

namespace Application.FeeRules.Validators
{
    // RuleType is immutable on update, so the per-type parameter requirements
    // (MinMonthsTogether/DaysBeforeDueDate) are re-checked in the service against the
    // existing rule's type rather than here.
    public class UpdateFeeRuleCommandValidator : AbstractValidator<UpdateFeeRuleCommand>
    {
        public UpdateFeeRuleCommandValidator()
        {
            RuleFor(command => command.Name)
                .NotEmpty()
                .MaximumLength(200);

            RuleFor(command => command.ValueType)
                .IsInEnum();

            RuleFor(command => command.Value)
                .GreaterThan(0);

            RuleFor(command => command.Value)
                .LessThanOrEqualTo(100)
                .When(command => command.ValueType == AwardValueType.Percentage)
                .WithMessage("A percentage value cannot exceed 100.");

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

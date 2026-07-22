using Application.Fees.Commands;
using FluentValidation;

namespace Application.Fees.Validators
{
    public class FeeStructureItemInputValidator : AbstractValidator<FeeStructureItemInput>
    {
        public FeeStructureItemInputValidator()
        {
            RuleFor(item => item.FeeCategoryCode)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(item => item.Amount)
                .GreaterThanOrEqualTo(0);

            RuleFor(item => item.FrequencyType)
                .IsInEnum();

            RuleFor(item => item.InstallmentCount)
                .InclusiveBetween(1, 12)
                .When(item => item.InstallmentCount.HasValue)
                .WithMessage("InstallmentCount must be between 1 and 12.");

            RuleFor(item => item.InstallmentCount)
                .Null()
                .When(item => item.FrequencyType != Domain.Enums.FeeFrequencyType.Annual)
                .WithMessage("InstallmentCount only applies to Annual fee items.");
        }
    }
}

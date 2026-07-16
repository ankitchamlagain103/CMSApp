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
        }
    }
}

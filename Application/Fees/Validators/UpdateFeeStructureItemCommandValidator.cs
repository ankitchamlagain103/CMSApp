using Application.Fees.Commands;
using FluentValidation;

namespace Application.Fees.Validators
{
    public class UpdateFeeStructureItemCommandValidator : AbstractValidator<UpdateFeeStructureItemCommand>
    {
        public UpdateFeeStructureItemCommandValidator()
        {
            RuleFor(item => item.Amount)
                .GreaterThanOrEqualTo(0);

            RuleFor(item => item.FrequencyType)
                .IsInEnum();
        }
    }
}

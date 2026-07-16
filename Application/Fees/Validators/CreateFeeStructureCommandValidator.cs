using Application.Fees.Commands;
using FluentValidation;

namespace Application.Fees.Validators
{
    public class CreateFeeStructureCommandValidator : AbstractValidator<CreateFeeStructureCommand>
    {
        public CreateFeeStructureCommandValidator()
        {
            RuleFor(command => command.AcademicClassId)
                .NotEmpty();

            RuleForEach(command => command.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.FeeCategoryCode).NotEmpty().MaximumLength(100);
                item.RuleFor(i => i.Amount).GreaterThanOrEqualTo(0);
                item.RuleFor(i => i.FrequencyType).IsInEnum();
            });
        }
    }
}

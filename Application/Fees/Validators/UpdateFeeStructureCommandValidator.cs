using Application.Fees.Commands;
using FluentValidation;

namespace Application.Fees.Validators
{
    public class UpdateFeeStructureCommandValidator : AbstractValidator<UpdateFeeStructureCommand>
    {
        public UpdateFeeStructureCommandValidator()
        {
            RuleFor(command => command.Status)
                .IsInEnum();
        }
    }
}

using Application.PayrollRuns.Commands;
using FluentValidation;

namespace Application.PayrollRuns.Validators
{
    public class UpdateSalarySlipLineCommandValidator : AbstractValidator<UpdateSalarySlipLineCommand>
    {
        public UpdateSalarySlipLineCommandValidator()
        {
            RuleFor(command => command.Description)
                .NotEmpty()
                .MaximumLength(300);

            RuleFor(command => command.Amount)
                .GreaterThan(0);
        }
    }
}

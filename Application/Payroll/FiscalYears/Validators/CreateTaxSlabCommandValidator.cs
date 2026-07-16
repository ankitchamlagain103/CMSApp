using Application.Payroll.FiscalYears.Commands;
using FluentValidation;

namespace Application.Payroll.FiscalYears.Validators
{
    public class CreateTaxSlabCommandValidator : AbstractValidator<CreateTaxSlabCommand>
    {
        public CreateTaxSlabCommandValidator()
        {
            RuleFor(command => command.MinAmount)
                .GreaterThanOrEqualTo(0);

            RuleFor(command => command.MaxAmount)
                .GreaterThan(command => command.MinAmount)
                .When(command => command.MaxAmount.HasValue);

            RuleFor(command => command.TaxRate)
                .InclusiveBetween(0, 1)
                    .WithMessage("TaxRate is a fraction between 0 and 1 (e.g. 0.1 for 10%).");

            RuleFor(command => command.SlabOrder)
                .GreaterThanOrEqualTo(0);
        }
    }
}

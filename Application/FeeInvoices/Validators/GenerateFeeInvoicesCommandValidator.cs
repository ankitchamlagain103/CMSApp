using Application.FeeInvoices.Commands;
using FluentValidation;

namespace Application.FeeInvoices.Validators
{
    public class GenerateFeeInvoicesCommandValidator : AbstractValidator<GenerateFeeInvoicesCommand>
    {
        public GenerateFeeInvoicesCommandValidator()
        {
            RuleFor(command => command.AcademicYearId)
                .NotEmpty();

            RuleFor(command => command.BillingYear)
                .InclusiveBetween(2000, 2100);

            RuleFor(command => command.BillingMonth)
                .InclusiveBetween(1, 12);
        }
    }
}

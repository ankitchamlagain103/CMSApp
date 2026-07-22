using Application.FeeInvoices.Commands;
using FluentValidation;

namespace Application.FeeInvoices.Validators
{
    public class FeeInvoiceLineInputValidator : AbstractValidator<FeeInvoiceLineInput>
    {
        public FeeInvoiceLineInputValidator()
        {
            RuleFor(line => line.Description)
                .NotEmpty()
                .MaximumLength(300);

            RuleFor(line => line.Amount)
                .NotEqual(0)
                .WithMessage("A manual line's amount cannot be zero (positive = charge, negative = credit).");

            RuleFor(line => line.FeeCategoryCode)
                .MaximumLength(100);
        }
    }
}

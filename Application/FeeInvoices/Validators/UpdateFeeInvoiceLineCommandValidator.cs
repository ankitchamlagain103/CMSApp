using Application.FeeInvoices.Commands;
using FluentValidation;

namespace Application.FeeInvoices.Validators
{
    public class UpdateFeeInvoiceLineCommandValidator : AbstractValidator<UpdateFeeInvoiceLineCommand>
    {
        public UpdateFeeInvoiceLineCommandValidator()
        {
            RuleFor(command => command.Description)
                .NotEmpty()
                .MaximumLength(300);

            RuleFor(command => command.Amount)
                .NotEqual(0)
                .WithMessage("A line's amount cannot be zero (positive = charge, negative = credit).");
        }
    }
}

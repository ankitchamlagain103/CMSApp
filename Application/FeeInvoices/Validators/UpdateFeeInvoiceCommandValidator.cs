using Application.FeeInvoices.Commands;
using FluentValidation;

namespace Application.FeeInvoices.Validators
{
    public class UpdateFeeInvoiceCommandValidator : AbstractValidator<UpdateFeeInvoiceCommand>
    {
        public UpdateFeeInvoiceCommandValidator()
        {
            RuleFor(command => command.DueDate)
                .NotEmpty();

            RuleFor(command => command.Remarks)
                .MaximumLength(500);
        }
    }
}

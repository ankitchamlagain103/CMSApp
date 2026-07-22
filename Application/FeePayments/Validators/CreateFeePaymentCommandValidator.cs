using Application.FeePayments.Commands;
using FluentValidation;

namespace Application.FeePayments.Validators
{
    public class CreateFeePaymentCommandValidator : AbstractValidator<CreateFeePaymentCommand>
    {
        public CreateFeePaymentCommandValidator()
        {
            RuleFor(command => command.EnrollmentId)
                .NotEmpty();

            RuleFor(command => command.PaymentDate)
                .NotEmpty();

            RuleFor(command => command.Amount)
                .GreaterThan(0);

            RuleFor(command => command.PaymentMode)
                .IsInEnum();

            RuleFor(command => command.ReferenceNo)
                .MaximumLength(100);

            RuleFor(command => command.Remarks)
                .MaximumLength(500);
        }
    }
}

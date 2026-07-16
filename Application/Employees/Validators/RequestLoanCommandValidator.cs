using Application.Employees.Commands;
using Domain.Constants;
using FluentValidation;

namespace Application.Employees.Validators
{
    public class RequestLoanCommandValidator : AbstractValidator<RequestLoanCommand>
    {
        public RequestLoanCommandValidator()
        {
            RuleFor(command => command.LoanTypeCode)
                .NotEmpty()
                .Must(code => code == LoanTypeCodes.Loan || code == LoanTypeCodes.Advance)
                    .WithMessage("LoanTypeCode must be 'LOAN' or 'ADVANCE'.");

            RuleFor(command => command.PrincipalAmount)
                .GreaterThan(0);

            RuleFor(command => command.EmiAmount)
                .GreaterThan(0)
                .LessThanOrEqualTo(command => command.PrincipalAmount)
                    .WithMessage("EmiAmount cannot exceed PrincipalAmount.");

            RuleFor(command => command.StartDate)
                .NotEqual(default(DateTime));

            RuleFor(command => command.Remarks)
                .MaximumLength(500);
        }
    }
}

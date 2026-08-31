using Application.Employees.Commands;
using FluentValidation;

namespace Application.Employees.Validators
{
    public class CreateLeaveRequestCommandValidator : AbstractValidator<CreateLeaveRequestCommand>
    {
        public CreateLeaveRequestCommandValidator()
        {
            RuleFor(command => command.LeaveTypeId)
                .NotEmpty();

            RuleFor(command => command.FromDate)
                .NotEmpty();

            RuleFor(command => command.ToDate)
                .NotEmpty()
                .GreaterThanOrEqualTo(command => command.FromDate)
                    .WithMessage("ToDate cannot be before FromDate.");

            RuleFor(command => command.Reason)
                .MaximumLength(1000);
        }
    }
}

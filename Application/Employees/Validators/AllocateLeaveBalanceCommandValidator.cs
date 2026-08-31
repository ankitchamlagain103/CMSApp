using Application.Employees.Commands;
using FluentValidation;

namespace Application.Employees.Validators
{
    public class AllocateLeaveBalanceCommandValidator : AbstractValidator<AllocateLeaveBalanceCommand>
    {
        public AllocateLeaveBalanceCommandValidator()
        {
            RuleFor(command => command.LeaveTypeId)
                .NotEmpty();

            RuleFor(command => command.Allocated)
                .GreaterThanOrEqualTo(0)
                .LessThanOrEqualTo(365);
        }
    }
}

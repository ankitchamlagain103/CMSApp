using Application.LeaveTypes.Commands;
using FluentValidation;

namespace Application.LeaveTypes.Validators
{
    public class UpdateLeaveTypeCommandValidator : AbstractValidator<UpdateLeaveTypeCommand>
    {
        public UpdateLeaveTypeCommandValidator()
        {
            RuleFor(command => command.Name)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(command => command.DaysPerYear)
                .GreaterThanOrEqualTo(0)
                .LessThanOrEqualTo(365);

            RuleFor(command => command.MaxConsecutiveDays)
                .GreaterThanOrEqualTo(1)
                .When(command => command.MaxConsecutiveDays.HasValue);

            RuleFor(command => command.MaxDaysPerWeek)
                .GreaterThanOrEqualTo(0)
                .LessThanOrEqualTo(7)
                .When(command => command.MaxDaysPerWeek.HasValue);

            RuleFor(command => command.MaxDaysPerMonth)
                .GreaterThanOrEqualTo(0)
                .LessThanOrEqualTo(31)
                .When(command => command.MaxDaysPerMonth.HasValue);
        }
    }
}

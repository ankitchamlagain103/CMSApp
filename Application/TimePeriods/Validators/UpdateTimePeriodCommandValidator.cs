using Application.TimePeriods.Commands;
using FluentValidation;

namespace Application.TimePeriods.Validators
{
    public class UpdateTimePeriodCommandValidator : AbstractValidator<UpdateTimePeriodCommand>
    {
        public UpdateTimePeriodCommandValidator()
        {
            RuleFor(command => command.Name)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(command => command.EndTime)
                .GreaterThan(command => command.StartTime)
                .WithMessage("EndTime must be after StartTime.");

            RuleFor(command => command.Kind)
                .IsInEnum();

            RuleFor(command => command.Order)
                .GreaterThanOrEqualTo(0);
        }
    }
}

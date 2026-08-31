using Application.TimePeriods.Commands;
using FluentValidation;

namespace Application.TimePeriods.Validators
{
    public class MapClassTimePeriodsCommandValidator : AbstractValidator<MapClassTimePeriodsCommand>
    {
        public MapClassTimePeriodsCommandValidator()
        {
            RuleFor(command => command.AcademicClassIds)
                .NotEmpty()
                .WithMessage("At least one AcademicClassId is required.");

            RuleFor(command => command.TimePeriodIds)
                .NotEmpty()
                .WithMessage("At least one TimePeriodId is required.");
        }
    }
}

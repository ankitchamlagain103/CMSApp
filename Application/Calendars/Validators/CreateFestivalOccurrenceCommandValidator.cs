using Application.Calendars.Commands;
using FluentValidation;

namespace Application.Calendars.Validators
{
    public class CreateFestivalOccurrenceCommandValidator : AbstractValidator<CreateFestivalOccurrenceCommand>
    {
        public CreateFestivalOccurrenceCommandValidator()
        {
            RuleFor(command => command.FestivalName)
                .NotEmpty()
                .MaximumLength(200);

            RuleFor(command => command.Category)
                .IsInEnum();

            RuleFor(command => command.BsYear)
                .InclusiveBetween(2000, 2200);

            RuleFor(command => command.BsStartMonth)
                .InclusiveBetween(1, 12);

            RuleFor(command => command.BsStartDay)
                .InclusiveBetween(1, 32);

            RuleFor(command => command.BsEndMonth)
                .InclusiveBetween(1, 12);

            RuleFor(command => command.BsEndDay)
                .InclusiveBetween(1, 32);

            RuleFor(command => command.BsEndMonth)
                .Must((command, endMonth) => endMonth > command.BsStartMonth || (endMonth == command.BsStartMonth && command.BsEndDay >= command.BsStartDay))
                .WithMessage("The festival's end date must not be before its start date (both within the same BS year).");

            RuleFor(command => command.Description)
                .MaximumLength(1000);

            RuleFor(command => command.ColorCode)
                .MaximumLength(20);
        }
    }
}

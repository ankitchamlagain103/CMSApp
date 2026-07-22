using Application.Calendars.Commands;
using FluentValidation;

namespace Application.Calendars.Validators
{
    public class UpdateCalendarEventCommandValidator : AbstractValidator<UpdateCalendarEventCommand>
    {
        public UpdateCalendarEventCommandValidator()
        {
            RuleFor(command => command.Title)
                .NotEmpty()
                .MaximumLength(200);

            RuleFor(command => command.EventType)
                .IsInEnum();

            RuleFor(command => command.AdDate)
                .NotNull()
                .When(command => !command.IsBsDate)
                .WithMessage("AdDate is required when the event is entered on the AD calendar.");

            RuleFor(command => command.BsYear)
                .NotNull()
                .When(command => command.IsBsDate)
                .WithMessage("BsYear is required when the event is entered on the BS calendar.");

            RuleFor(command => command.BsMonth)
                .NotNull()
                .InclusiveBetween(1, 12)
                .When(command => command.IsBsDate)
                .WithMessage("BsMonth (1-12) is required when the event is entered on the BS calendar.");

            RuleFor(command => command.BsDay)
                .NotNull()
                .InclusiveBetween(1, 32)
                .When(command => command.IsBsDate)
                .WithMessage("BsDay (1-32) is required when the event is entered on the BS calendar.");

            RuleFor(command => command.Description)
                .MaximumLength(1000);

            RuleFor(command => command.IconKey)
                .MaximumLength(100);

            RuleFor(command => command.ColorCode)
                .MaximumLength(20);

            RuleFor(command => command.Language)
                .MaximumLength(10);
        }
    }
}

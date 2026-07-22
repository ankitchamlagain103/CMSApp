using Application.Meetings.Commands;
using FluentValidation;

namespace Application.Meetings.Validators
{
    public class ScheduleMeetingCommandValidator : AbstractValidator<ScheduleMeetingCommand>
    {
        public ScheduleMeetingCommandValidator()
        {
            RuleFor(command => command.Title)
                .NotEmpty()
                .MaximumLength(250);

            RuleFor(command => command.Description)
                .MaximumLength(2000);

            RuleFor(command => command.ScheduledAdDate)
                .NotNull()
                .When(command => !command.IsBsScheduled)
                .WithMessage("ScheduledAdDate is required when the meeting is scheduled on the AD calendar.");

            RuleFor(command => command.ScheduledBsYear)
                .NotNull()
                .When(command => command.IsBsScheduled)
                .WithMessage("ScheduledBsYear is required when the meeting is scheduled on the BS calendar.");

            RuleFor(command => command.ScheduledBsMonth)
                .NotNull()
                .InclusiveBetween(1, 12)
                .When(command => command.IsBsScheduled)
                .WithMessage("ScheduledBsMonth (1-12) is required when the meeting is scheduled on the BS calendar.");

            RuleFor(command => command.ScheduledBsDay)
                .NotNull()
                .InclusiveBetween(1, 32)
                .When(command => command.IsBsScheduled)
                .WithMessage("ScheduledBsDay (1-32) is required when the meeting is scheduled on the BS calendar.");

            RuleFor(command => command.StartTime)
                .GreaterThanOrEqualTo(TimeSpan.Zero)
                .LessThan(TimeSpan.FromHours(24))
                .WithMessage("StartTime must be a wall-clock time between 00:00:00 and 23:59:59.");

            RuleFor(command => command.EndTime)
                .GreaterThan(command => command.StartTime)
                .WithMessage("EndTime must be after StartTime.");

            RuleFor(command => command.EndTime)
                .LessThanOrEqualTo(TimeSpan.FromHours(24))
                .WithMessage("EndTime must not pass midnight -- a meeting is scheduled within one calendar day.");

            RuleFor(command => command.Location)
                .MaximumLength(500);

            RuleForEach(command => command.AttendeeEmails)
                .NotEmpty()
                .EmailAddress()
                .WithMessage("Every attendee entry must be a valid email address.");
        }
    }
}

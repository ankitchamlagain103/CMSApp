using Application.Meetings.Commands;
using FluentValidation;

namespace Application.Meetings.Validators
{
    public class RespondInvitationCommandValidator : AbstractValidator<RespondInvitationCommand>
    {
        public RespondInvitationCommandValidator()
        {
            RuleFor(command => command.MeetingId)
                .NotEmpty();

            RuleFor(command => command.Email)
                .NotEmpty()
                .EmailAddress();

            RuleFor(command => command.Status)
                .IsInEnum();
        }
    }
}

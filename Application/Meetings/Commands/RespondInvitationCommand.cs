using Domain.Enums;

namespace Application.Meetings.Commands
{
    public class RespondInvitationCommand
    {
        public Guid MeetingId { get; set; }
        public string Email { get; set; }
        public AttendeeStatus Status { get; set; }
    }
}

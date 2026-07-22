using Domain.Enums;

namespace Application.Meetings.Dtos
{
    public class MeetingAttendeeDto
    {
        public Guid Id { get; set; }
        public Guid? UserId { get; set; }
        public string Email { get; set; }
        public AttendeeStatus Status { get; set; }
    }
}

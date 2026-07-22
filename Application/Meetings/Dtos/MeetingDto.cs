namespace Application.Meetings.Dtos
{
    public class MeetingDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime AdDate { get; set; }
        public int BsYear { get; set; }
        public int BsMonth { get; set; }
        public int BsDay { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool IsVirtual { get; set; }
        public string Location { get; set; }
        public Guid HostUserId { get; set; }
        public List<MeetingAttendeeDto> Attendees { get; set; } = new List<MeetingAttendeeDto>();
    }
}

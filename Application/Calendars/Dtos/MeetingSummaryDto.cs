namespace Application.Calendars.Dtos
{
    public class MeetingSummaryDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool IsVirtual { get; set; }
        public string Location { get; set; }
    }
}

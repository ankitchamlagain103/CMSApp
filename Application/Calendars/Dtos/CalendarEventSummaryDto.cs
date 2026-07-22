using Domain.Enums;

namespace Application.Calendars.Dtos
{
    public class CalendarEventSummaryDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public CalendarEventType EventType { get; set; }
        public string ColorCode { get; set; }
        public string IconKey { get; set; }
    }
}

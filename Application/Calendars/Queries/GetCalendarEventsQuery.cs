using Domain.Enums;

namespace Application.Calendars.Queries
{
    public class GetCalendarEventsQuery
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public CalendarEventType? EventType { get; set; }
        public DateTime? FromAdDate { get; set; }
        public DateTime? ToAdDate { get; set; }
        public int? BsYear { get; set; }
        public bool? IsActive { get; set; }
    }
}

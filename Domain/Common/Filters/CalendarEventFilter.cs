using Domain.Enums;

namespace Domain.Common.Filters
{
    public class CalendarEventFilter
    {
        public CalendarEventType? EventType { get; set; }
        public DateTime? FromAdDate { get; set; }
        public DateTime? ToAdDate { get; set; }
        public int? BsYear { get; set; }
        public bool? IsActive { get; set; }
    }
}

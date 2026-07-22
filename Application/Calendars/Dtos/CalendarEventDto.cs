using Domain.Enums;

namespace Application.Calendars.Dtos
{
    public class CalendarEventDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public CalendarEventType EventType { get; set; }
        public DateTime AdDate { get; set; }
        public int BsYear { get; set; }
        public int BsMonth { get; set; }
        public int BsDay { get; set; }
        public string Description { get; set; }
        public string IconKey { get; set; }
        public string ColorCode { get; set; }
        public string Language { get; set; }
        public bool IsActive { get; set; }
    }
}

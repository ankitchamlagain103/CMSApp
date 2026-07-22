namespace Application.Calendars.Dtos
{
    public class CalendarDayDto
    {
        public DateTime AdDate { get; set; }
        public int AdYear { get; set; }
        public int AdMonth { get; set; }
        public int AdDay { get; set; }
        public int BsYear { get; set; }
        public int BsMonth { get; set; }
        public int BsDay { get; set; }
        public int DayOfWeekIndex { get; set; }
        public string DayNameEn { get; set; }
        public string DayNameNp { get; set; }
        public bool IsWeeklyHoliday { get; set; }
        public bool IsToday { get; set; }
        public List<CalendarEventSummaryDto> Events { get; set; } = new List<CalendarEventSummaryDto>();
        public List<FestivalSummaryDto> Festivals { get; set; } = new List<FestivalSummaryDto>();
        public List<MeetingSummaryDto> Meetings { get; set; } = new List<MeetingSummaryDto>();
    }
}

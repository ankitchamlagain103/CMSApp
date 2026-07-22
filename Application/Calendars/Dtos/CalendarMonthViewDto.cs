namespace Application.Calendars.Dtos
{
    public class CalendarMonthViewDto
    {
        // "BS" or "AD" -- echoes the requested mode.
        public string Mode { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthNameEn { get; set; }
        public string MonthNameNp { get; set; }
        public int TotalDays { get; set; }
        public DateTime StartAdDate { get; set; }
        public DateTime EndAdDate { get; set; }
        public List<CalendarDayDto> Days { get; set; } = new List<CalendarDayDto>();
    }
}

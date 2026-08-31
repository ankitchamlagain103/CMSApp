namespace Application.Calendars.Dtos
{
    public class CalendarYearViewDto
    {
        // "BS" or "AD" -- echoes the requested mode.
        public string Mode { get; set; }
        public int Year { get; set; }
        public List<CalendarMonthViewDto> Months { get; set; } = new List<CalendarMonthViewDto>();
    }
}

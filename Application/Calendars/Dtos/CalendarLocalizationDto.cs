namespace Application.Calendars.Dtos
{
    public class CalendarLocalizationDto
    {
        public List<BsMonthNameDto> Months { get; set; } = new List<BsMonthNameDto>();
        public List<BsWeekdayNameDto> Weekdays { get; set; } = new List<BsWeekdayNameDto>();
    }
}

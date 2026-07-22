namespace Application.Calendars.Dtos
{
    public class BsWeekdayNameDto
    {
        public Guid Id { get; set; }
        public int WeekdayIndex { get; set; }
        public string NameEn { get; set; }
        public string NameNp { get; set; }
        public bool IsWeeklyHoliday { get; set; }
    }
}

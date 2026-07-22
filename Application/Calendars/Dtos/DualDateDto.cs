namespace Application.Calendars.Dtos
{
    // One calendar day expressed in both systems -- the response shape of the today/convert
    // endpoints, handy for date pickers that let the user type either calendar.
    public class DualDateDto
    {
        public DateTime AdDate { get; set; }
        public int BsYear { get; set; }
        public int BsMonth { get; set; }
        public int BsDay { get; set; }
        public string BsMonthNameEn { get; set; }
        public string BsMonthNameNp { get; set; }
        public int DayOfWeekIndex { get; set; }
        public string DayNameEn { get; set; }
        public string DayNameNp { get; set; }
    }
}

namespace Application.Calendars.Dtos
{
    public class BsMonthLengthDto
    {
        public Guid Id { get; set; }
        public int BsYear { get; set; }
        public int BsMonth { get; set; }
        public int DaysInMonth { get; set; }
    }
}

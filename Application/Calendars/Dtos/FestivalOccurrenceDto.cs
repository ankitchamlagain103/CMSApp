using Domain.Enums;

namespace Application.Calendars.Dtos
{
    public class FestivalOccurrenceDto
    {
        public Guid Id { get; set; }
        public string FestivalName { get; set; }
        public HolidayType Category { get; set; }
        public int BsYear { get; set; }
        public int BsStartMonth { get; set; }
        public int BsStartDay { get; set; }
        public int BsEndMonth { get; set; }
        public int BsEndDay { get; set; }
        public DateTime AdStartDate { get; set; }
        public DateTime AdEndDate { get; set; }
        public string Description { get; set; }
        public string ColorCode { get; set; }
        public bool IsActive { get; set; }
    }
}

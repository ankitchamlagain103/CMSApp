using Domain.Enums;

namespace Application.Calendars.Commands
{
    public class UpdateFestivalOccurrenceCommand
    {
        public string FestivalName { get; set; }
        public HolidayType Category { get; set; }
        public int BsYear { get; set; }
        public int BsStartMonth { get; set; }
        public int BsStartDay { get; set; }
        public int BsEndMonth { get; set; }
        public int BsEndDay { get; set; }
        public string Description { get; set; }
        public string ColorCode { get; set; }
        public bool IsActive { get; set; } = true;
    }
}

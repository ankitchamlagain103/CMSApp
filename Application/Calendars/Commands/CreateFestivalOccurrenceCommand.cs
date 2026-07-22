using Domain.Enums;

namespace Application.Calendars.Commands
{
    // Festival dates are entered in BS only (they are BS-anchored facts); the service computes
    // and stores the denormalized AD range. The whole occurrence must fall inside one BS year
    // -- a festival crossing the BS new year is modeled as two occurrences.
    public class CreateFestivalOccurrenceCommand
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

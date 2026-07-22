using Domain.Enums;

namespace Application.Calendars.Dtos
{
    public class FestivalSummaryDto
    {
        public Guid Id { get; set; }
        public string FestivalName { get; set; }
        public HolidayType Category { get; set; }
        public string ColorCode { get; set; }

        // True on the day the festival's AD span starts/ends -- lets the UI draw range caps.
        public bool IsStartDay { get; set; }
        public bool IsEndDay { get; set; }
    }
}

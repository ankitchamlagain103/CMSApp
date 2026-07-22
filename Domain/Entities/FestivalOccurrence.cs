using Domain.Enums;

namespace Domain.Entities
{
    // A festival's dates for one specific BS year (Dashain, Tihar, etc. shift every year,
    // unlike a fixed-date CalendarEvent). The BS start/end fields are the source of truth an
    // admin edits; AdStartDate/AdEndDate are denormalized -- computed via
    // IBsAdConversionService when the row is created/updated, purely so AD date-range queries
    // don't need to re-run the conversion per request.
    public class FestivalOccurrence : SoftDeleteAuditableEntity
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
        public bool IsActive { get; set; } = true;
    }
}

namespace Domain.Entities
{
    // Weekday display name (English/Nepali), index 0=Sunday..6=Saturday matching
    // JS Date.getDay() and .NET DayOfWeek. Reference data -- 7 rows seeded by CalendarSeeder.
    public class BsWeekdayName : AuditableEntity
    {
        public Guid Id { get; set; }
        public int WeekdayIndex { get; set; }
        public string NameEn { get; set; }
        public string NameNp { get; set; }

        // True if this weekday is a standing weekly holiday (Saturday in Nepal) -- drives the
        // frontend's weekend highlighting instead of a hardcoded day-of-week check.
        public bool IsWeeklyHoliday { get; set; }
    }
}

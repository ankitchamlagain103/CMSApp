namespace Domain.Entities
{
    // BS month display name (English/Nepali) for one of the 12 months. Reference data -- 12 rows
    // seeded by CalendarSeeder, names editable but rows never added/removed.
    public class BsMonthName : AuditableEntity
    {
        public Guid Id { get; set; }
        public int MonthNumber { get; set; }
        public string NameEn { get; set; }
        public string NameNp { get; set; }
    }
}

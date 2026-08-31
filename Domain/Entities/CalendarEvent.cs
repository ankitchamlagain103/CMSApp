using Domain.Enums;

namespace Domain.Entities
{
    // A single-date event/observance/note (Father's Day, Constitution Day, a bilingual day
    // description, etc.) -- the "day note" concept is covered via CalendarEventType.Note.
    // AdDate is canonical (date column, no time-of-day meaning); BsYear/BsMonth/BsDay are
    // denormalized, computed via IBsAdConversionService on save so month-view queries and
    // BS-mode rendering never re-run the conversion.
    //
    // 2026-07-23: ProvinceCode/BranchCode (both Config-catalog codes, ConfigTypeCodes.Province/
    // Branch) let a PublicHoliday be scoped to a specific province/branch instead of applying
    // school-wide -- both null means "everywhere". StudentId/EmployeeId (plain scalar Guids, real
    // nav properties since both are Domain entities -- unlike Meeting.HostUserId, which stays a
    // scalar because ApplicationUser is an Infrastructure type) let a StudentBirthday/
    // EmployeeBirthday event pin onto a specific person; only ever set together with the matching
    // EventType, never both at once.
    public class CalendarEvent : SoftDeleteAuditableEntity
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public CalendarEventType EventType { get; set; }
        public DateTime AdDate { get; set; }
        public int BsYear { get; set; }
        public int BsMonth { get; set; }
        public int BsDay { get; set; }
        public string Description { get; set; }
        public string IconKey { get; set; }
        public string ColorCode { get; set; }
        public string Language { get; set; } = "en";
        public bool IsActive { get; set; } = true;
        public string ProvinceCode { get; set; }
        public string BranchCode { get; set; }
        public Guid? StudentId { get; set; }
        public Guid? EmployeeId { get; set; }

        public virtual Student Student { get; set; }
        public virtual Employee Employee { get; set; }
    }
}

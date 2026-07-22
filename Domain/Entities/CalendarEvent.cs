using Domain.Enums;

namespace Domain.Entities
{
    // A single-date event/observance/note (Father's Day, Constitution Day, a bilingual day
    // description, etc.) -- the "day note" concept is covered via CalendarEventType.Note.
    // AdDate is canonical (date column, no time-of-day meaning); BsYear/BsMonth/BsDay are
    // denormalized, computed via IBsAdConversionService on save so month-view queries and
    // BS-mode rendering never re-run the conversion.
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
    }
}

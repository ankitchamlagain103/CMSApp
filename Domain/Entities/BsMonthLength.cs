namespace Domain.Entities
{
    // Number of days in one BS month of one BS year (Baisakh=1..Chaitra=12). One row per
    // (BsYear, BsMonth) pair -- the DB-backed replacement for a hardcoded BS year-data
    // dictionary. BS month lengths are set by yearly government publication, not by formula,
    // so this table is admin-editable: a new BS year's official lengths are added via the
    // calendar-configuration endpoint without a code deployment. Hard-deleted reference data.
    public class BsMonthLength : AuditableEntity
    {
        public Guid Id { get; set; }
        public int BsYear { get; set; }
        public int BsMonth { get; set; }
        public int DaysInMonth { get; set; }
    }
}

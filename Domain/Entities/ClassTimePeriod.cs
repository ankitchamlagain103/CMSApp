namespace Domain.Entities
{
    // Links an AcademicClass to a TimePeriod it uses -- this is the "certain classes can have
    // different periods" mapping: a class only ever picks (via Exam.TimePeriodId /
    // TeacherAssignment.TimePeriodId) from the TimePeriod rows mapped to its own class here.
    // Bulk-created via TimePeriodService.MapClassTimePeriodsAsync (POST /api/timeperiods/map),
    // the cross-product of several classes x several periods in one call -- one school routine
    // for Nursery-Five, a different one for Six-Twelve, is two separate bulk-map calls.
    // Pure link row, hard-deleted, same convention as TeacherAssignment/ClassSubject links.
    public class ClassTimePeriod : AuditableEntity
    {
        public Guid Id { get; set; }
        public Guid AcademicClassId { get; set; }
        public Guid TimePeriodId { get; set; }
        public virtual AcademicClass AcademicClass { get; set; }
        public virtual TimePeriod TimePeriod { get; set; }
    }
}

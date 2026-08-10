namespace Application.Employees.Commands
{
    // Optimized multi-section counterpart to AssignTeacherCommand -- assigns the same
    // ClassSubject (and, when set, TimePeriodId) to an employee across several sections in one
    // call, instead of repeating the whole assign flow once per section. IsClassTeacher is only
    // valid when exactly one ClassSectionId is supplied (a class teacher belongs to one section)
    // -- rejected upfront as a ValidationError, not per-item, since it's a request-shape error,
    // not a business conflict to skip past.
    //
    // If TimePeriodId is set, it's shared by every section in the list -- since an employee can't
    // teach two different sections during the identical period (2026-08-04), only the FIRST
    // section actually saves and every one after it is skipped as a time-period conflict. Set
    // TimePeriodId only when assigning a single section, or use the class-scoped/employee-scoped
    // bulk-entry endpoints instead, where each row can carry its own TimePeriodId.
    public class AssignTeacherBulkCommand
    {
        public Guid ClassSubjectId { get; set; }
        public List<Guid> ClassSectionIds { get; set; } = new List<Guid>();
        public bool IsClassTeacher { get; set; }
        public Guid? TimePeriodId { get; set; }
    }
}

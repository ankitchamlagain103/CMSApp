namespace Application.Teachers.Commands
{
    // ClassSectionId is REQUIRED for a class-wide subject (2026-08-04) -- a teacher cannot teach
    // every section of a class at once, so it's no longer valid to leave this null to mean "every
    // section." Stays a nullable Guid? here only because a section-scoped subject derives its
    // section automatically (the caller may omit it in that one case) -- the actual requiredness
    // check lives in TeacherAssignmentBuilder.BuildAsync, the single place this rule is enforced
    // for every entry point (single/bulk/bulk-entry, teacher-scoped and class-scoped alike).
    public class AssignTeacherCommand
    {
        public Guid ClassSubjectId { get; set; }
        public Guid? ClassSectionId { get; set; }
        public bool IsClassTeacher { get; set; }

        // Optional TimePeriod id (Domain/Entities/TimePeriod) -- which routine slot this teacher
        // teaches this class/subject in. Validated in the service: must be a Period (not Break)
        // and mapped, via ClassTimePeriod, to the assignment's own class.
        public Guid? TimePeriodId { get; set; }
    }
}

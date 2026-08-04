namespace Application.Teachers.Commands
{
    // One row of a bulk-entry grid -- unlike AssignTeacherBulkCommand (which fixes ClassSubjectId/
    // TimePeriodId for the call and only varies the section list), each item here carries its own
    // full set of fields, so a teacher's whole routine (several classes/subjects/sections/periods)
    // can be entered in one submission instead of one call per row.
    //
    // ClassSectionId is REQUIRED for a class-wide subject (2026-08-04, see
    // TeacherAssignmentBuilder.BuildAsync) -- a teacher can't teach every section at once. Stays
    // Guid? here only because a section-scoped subject derives its section automatically.
    public class TeacherAssignmentEntryItem
    {
        public Guid ClassSubjectId { get; set; }
        public Guid? ClassSectionId { get; set; }
        public bool IsClassTeacher { get; set; }
        public Guid? TimePeriodId { get; set; }
    }
}

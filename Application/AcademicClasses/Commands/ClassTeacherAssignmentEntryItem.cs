namespace Application.AcademicClasses.Commands
{
    // One row of the "who teaches this class" bulk-entry grid -- unlike
    // Application.Teachers.Commands.TeacherAssignmentEntryItem (scoped to one teacher via the
    // route), TeacherId is part of the row itself here, since one class is taught by several
    // different teachers across its subjects/sections.
    //
    // ClassSectionId is REQUIRED for a class-wide subject (2026-08-04, see
    // TeacherAssignmentBuilder.BuildAsync) -- a teacher can't teach every section at once. Stays
    // Guid? here only because a section-scoped subject derives its section automatically.
    public class ClassTeacherAssignmentEntryItem
    {
        public Guid TeacherId { get; set; }
        public Guid ClassSubjectId { get; set; }
        public Guid? ClassSectionId { get; set; }
        public bool IsClassTeacher { get; set; }
        public Guid? TimePeriodId { get; set; }
    }
}

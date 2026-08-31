namespace Application.AcademicClasses.Dtos
{
    // ItemIndex correlates back to the submitted Items[] row (0-based) -- TeacherId/
    // ClassSubjectId/ClassSectionId alone can't always do that, since two rows in the same
    // request can legitimately name the same combination (the second one is exactly what gets
    // reported as a duplicate).
    public class ClassTeacherAssignmentEntrySkipDto
    {
        public int ItemIndex { get; set; }
        public Guid TeacherId { get; set; }
        public Guid ClassSubjectId { get; set; }
        public Guid? ClassSectionId { get; set; }
        public string Reason { get; set; }
    }
}

namespace Application.Employees.Dtos
{
    // ItemIndex correlates back to the submitted Items[] row (0-based) -- ClassSubjectId/
    // ClassSectionId alone can't always do that, since two rows in the same request can
    // legitimately name the same pair (the second one is exactly what gets reported as a
    // duplicate).
    public class TeacherAssignmentEntrySkipDto
    {
        public int ItemIndex { get; set; }
        public Guid ClassSubjectId { get; set; }
        public Guid? ClassSectionId { get; set; }
        public string Reason { get; set; }
    }
}

namespace Application.Teachers.Dtos
{
    // ClassSectionId/SectionCode are null when the assignment covers every section of the class.
    public class TeacherAssignmentDto
    {
        public Guid Id { get; set; }
        public Guid TeacherId { get; set; }
        public Guid ClassSubjectId { get; set; }
        public Guid AcademicClassId { get; set; }
        public string SubjectCode { get; set; }
        public Guid? ClassSectionId { get; set; }
        public string SectionCode { get; set; }
        public bool IsClassTeacher { get; set; }
    }
}

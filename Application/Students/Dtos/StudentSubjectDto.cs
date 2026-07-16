namespace Application.Students.Dtos
{
    // One subject the student is studying: every mandatory subject of their class/section plus
    // the electives chosen on the enrollment. TeacherName is who teaches it to the student's
    // section (section-scoped assignments win over all-section ones; comma-joined when several
    // teachers share it; null when nobody is assigned yet).
    public class StudentSubjectDto
    {
        public Guid ClassSubjectId { get; set; }
        public string SubjectCode { get; set; }
        public bool IsMandatory { get; set; }
        public int DisplayOrder { get; set; }
        public string TeacherName { get; set; }
    }
}

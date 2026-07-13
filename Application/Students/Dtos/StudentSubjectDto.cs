namespace Application.Students.Dtos
{
    // One subject the student is studying: every mandatory subject of their class/section plus
    // the electives chosen on the enrollment.
    public class StudentSubjectDto
    {
        public Guid ClassSubjectId { get; set; }
        public string SubjectCode { get; set; }
        public bool IsMandatory { get; set; }
        public int DisplayOrder { get; set; }
    }
}

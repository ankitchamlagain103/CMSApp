namespace Application.Enrollments.Dtos
{
    public class EnrollmentSubjectDto
    {
        public Guid Id { get; set; }
        public Guid EnrollmentId { get; set; }
        public Guid ClassSubjectId { get; set; }
        public string SubjectCode { get; set; }
    }
}

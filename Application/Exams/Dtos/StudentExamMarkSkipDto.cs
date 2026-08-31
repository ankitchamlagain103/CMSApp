namespace Application.Exams.Dtos
{
    public class StudentExamMarkSkipDto
    {
        public Guid EnrollmentId { get; set; }
        public string Reason { get; set; }
    }
}

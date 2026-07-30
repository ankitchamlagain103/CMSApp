namespace Application.Exams.Dtos
{
    public class ExamResultGenerationSkipDto
    {
        public Guid EnrollmentId { get; set; }
        public string StudentName { get; set; }
        public string Reason { get; set; }
    }
}

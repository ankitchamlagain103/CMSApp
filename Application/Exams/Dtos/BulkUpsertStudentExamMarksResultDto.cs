namespace Application.Exams.Dtos
{
    public class BulkUpsertStudentExamMarksResultDto
    {
        public Guid ExamId { get; set; }
        public int UpsertedCount { get; set; }
        public List<StudentExamMarkSkipDto> Skipped { get; set; } = new List<StudentExamMarkSkipDto>();
    }
}

namespace Application.Exams.Commands
{
    public class BulkUpsertStudentExamMarksCommand
    {
        public Guid ExamId { get; set; }
        public List<StudentExamMarkLineInput> Marks { get; set; } = new List<StudentExamMarkLineInput>();
    }
}

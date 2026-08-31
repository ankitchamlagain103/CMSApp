using Domain.Enums;

namespace Application.Exams.Commands
{
    public class CreateExamTermCommand
    {
        public Guid AcademicYearId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public int Sequence { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool PublishResult { get; set; }
        public ExamTermStatus Status { get; set; } = ExamTermStatus.Draft;
    }
}

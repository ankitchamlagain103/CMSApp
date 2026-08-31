using Domain.Enums;

namespace Application.Exams.Commands
{
    // AcademicYearId/Code are identity-like and immutable -- same convention as
    // UpdateAcademicClassCommand only allowing Status.
    public class UpdateExamTermCommand
    {
        public string Name { get; set; }
        public int Sequence { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool PublishResult { get; set; }
        public ExamTermStatus Status { get; set; }
    }
}

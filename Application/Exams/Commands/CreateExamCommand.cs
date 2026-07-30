namespace Application.Exams.Commands
{
    // One exam per (ExamTermId, ClassSubjectId) -- per the revised design, an exam always covers
    // the whole grade (no ClassSectionId), and there is no free-form Name/WeightagePercent/
    // IsFinalExam anymore (no more than one graded sitting per subject per term). No Room --
    // the seat-arrangement subsystem was removed (2026-07-30); InvigilatorEmployeeId stays
    // optional (a real Employee, not needed for the simple "assign subject, date, time" flow).
    public class CreateExamCommand
    {
        public Guid ExamTermId { get; set; }
        public Guid ClassSubjectId { get; set; }
        public DateTime ExamDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public Guid? InvigilatorEmployeeId { get; set; }
        public string Remarks { get; set; }
    }
}

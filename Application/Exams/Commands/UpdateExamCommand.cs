namespace Application.Exams.Commands
{
    // ExamTermId/ClassSubjectId are identity-like and immutable -- re-targeting an exam at a
    // different term/subject means creating a new one (same convention as UpdateClassSubjectCommand
    // leaving SubjectCode/ClassSectionId out).
    //
    // Class period timing (2026-07-30, TimePeriodId 2026-08-03) -- see CreateExamCommand's doc
    // comment for the TimePeriodId-vs-raw-time rule; identical here.
    public class UpdateExamCommand
    {
        public DateTime ExamDate { get; set; }
        public Guid? TimePeriodId { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public string Remarks { get; set; }
    }
}

namespace Application.Exams.Commands
{
    // One subject's sitting within a SaveExamRoutineCommand -- ClassSubjectId must belong to
    // the command's own AcademicClassId (checked in the service, not here).
    //
    // Class period timing (2026-07-30, TimePeriodId 2026-08-03) -- see CreateExamCommand's doc
    // comment for the TimePeriodId-vs-raw-time rule; identical here, per item.
    public class ExamRoutineItemInput
    {
        public Guid ClassSubjectId { get; set; }
        public DateTime ExamDate { get; set; }
        public Guid? TimePeriodId { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public string Remarks { get; set; }
    }
}

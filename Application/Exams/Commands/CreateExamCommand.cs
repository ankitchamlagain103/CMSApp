namespace Application.Exams.Commands
{
    // One exam per (ExamTermId, ClassSubjectId) -- per the revised design, an exam always covers
    // the whole grade (no ClassSectionId), and there is no free-form Name/WeightagePercent/
    // IsFinalExam anymore (no more than one graded sitting per subject per term). No Room and no
    // invigilator -- both removed 2026-07-30, per instruction; an exam is just subject + date/time.
    //
    // Class period timing (2026-07-30, moved off the Config catalog onto a real TimePeriod FK
    // 2026-08-03): either send TimePeriodId (a Domain/Entities/TimePeriod id -- must be a Period,
    // not a Break, and mapped to this exam's class via ClassTimePeriod) and leave
    // StartTime/EndTime null -- the service resolves them from the period's own start/end time --
    // or send StartTime/EndTime directly and leave TimePeriodId null. Exactly one of the two
    // paths is required (validator-enforced).
    public class CreateExamCommand
    {
        public Guid ExamTermId { get; set; }
        public Guid ClassSubjectId { get; set; }
        public DateTime ExamDate { get; set; }
        public Guid? TimePeriodId { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public string Remarks { get; set; }
    }
}

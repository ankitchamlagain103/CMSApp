namespace Application.Exams.Commands
{
    // The full batch-scheduling workflow for one class within one exam term: the caller submits
    // every subject's date/time/remarks in one grid, and the save is an idempotent SYNC against
    // whatever Exam rows already exist for this (ExamTermId, AcademicClassId) pair -- a subject
    // present in Items and already scheduled is updated in place, a subject present but not yet
    // scheduled is created, and an existing scheduled subject missing from Items is removed
    // (hard-deleted, unless it already has recorded marks, which fails the whole save instead).
    // Re-submitting the same Items produces the same end state every time.
    //
    // Validated as a single atomic transaction (Application/Exams/ExamService.SaveExamRoutineAsync):
    // every item's exam date must fall within the exam term's own date range, and no two subjects
    // for this class may be scheduled at overlapping times on the same date. Any violation fails
    // the whole request -- nothing is partially saved.
    public class SaveExamRoutineCommand
    {
        public Guid ExamTermId { get; set; }
        public Guid AcademicClassId { get; set; }
        public List<ExamRoutineItemInput> Items { get; set; } = new List<ExamRoutineItemInput>();
    }
}

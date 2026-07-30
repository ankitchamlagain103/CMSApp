namespace Application.Exams.Commands
{
    // Schedules an Exam for every listed subject of one class within one exam term, in a single
    // call -- the "set the whole routine at once" flow the UI needs instead of one POST per
    // subject. Each item still carries its own date/time/invigilator (different subjects sit on
    // different days), so this is a bulk create over CreateExamCommand's shape, not a single
    // schedule broadcast across subjects. Skip-list style, same convention as
    // BulkUpsertStudentExamMarksCommand/CreateBulkFeeAdjustmentCommand -- a bad item is reported
    // in the result's Skipped list rather than failing the whole request.
    public class CreateExamRoutineCommand
    {
        public Guid ExamTermId { get; set; }
        public Guid AcademicClassId { get; set; }
        public List<ExamRoutineItemInput> Items { get; set; } = new List<ExamRoutineItemInput>();
    }
}

namespace Application.Exams.Commands
{
    // ExamTermId/ClassSubjectId are identity-like and immutable -- re-targeting an exam at a
    // different term/subject means creating a new one (same convention as UpdateClassSubjectCommand
    // leaving SubjectCode/ClassSectionId out).
    public class UpdateExamCommand
    {
        public DateTime ExamDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public Guid? InvigilatorEmployeeId { get; set; }
        public string Remarks { get; set; }
    }
}

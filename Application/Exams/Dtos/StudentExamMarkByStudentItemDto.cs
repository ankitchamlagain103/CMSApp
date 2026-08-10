namespace Application.Exams.Dtos
{
    // One row per subject/exam a single enrollment is eligible for within an exam term -- the
    // admin, student-wise marks-entry worklist (the counterpart to ExamMarkRosterItemDto's
    // teacher-wise, per-exam roster: this is "one student, every subject" instead of "one subject,
    // every student"). Mark is null when the student hasn't been marked yet for that exam.
    // FullMarks/etc. are the same read-only pass-through ExamDto already carries, repeated here so
    // the UI can render the whole entry grid from this one call.
    public class StudentExamMarkByStudentItemDto
    {
        public Guid ExamId { get; set; }
        public Guid ClassSubjectId { get; set; }
        public string SubjectCode { get; set; }
        public string SubjectLabel { get; set; }
        public DateTime ExamDate { get; set; }
        public bool MarksLocked { get; set; }
        public int? FullMarks { get; set; }
        public int? PassMarks { get; set; }
        public bool HasTheory { get; set; }
        public bool HasPractical { get; set; }
        public int? TheoryMarks { get; set; }
        public int? PracticalMarks { get; set; }
        public int? TheoryPassMarks { get; set; }
        public int? PracticalPassMarks { get; set; }
        public StudentExamMarkDto Mark { get; set; }
    }
}

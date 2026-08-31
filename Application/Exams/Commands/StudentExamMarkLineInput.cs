namespace Application.Exams.Commands
{
    // One student's row within a BulkUpsertStudentExamMarksCommand -- the realistic teacher UX
    // (enter a whole roster's marks for one exam schedule in one call). Upserts: an existing row
    // for (ExamScheduleId, EnrollmentId) is updated in place, a missing one is created.
    public class StudentExamMarkLineInput
    {
        public Guid EnrollmentId { get; set; }
        public decimal? TheoryObtainedMarks { get; set; }
        public decimal? PracticalObtainedMarks { get; set; }
        public decimal? InternalMarks { get; set; }
        public decimal TheoryGraceMarks { get; set; }
        public decimal PracticalGraceMarks { get; set; }
        public bool TheoryAbsent { get; set; }
        public bool PracticalAbsent { get; set; }
        public string Remarks { get; set; }
    }
}

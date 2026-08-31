namespace Application.Exams.Commands
{
    // ExamScheduleId/EnrollmentId are identity-like and immutable -- re-targeting a mark row to a
    // different schedule/student means creating a new one.
    public class UpdateStudentExamMarkCommand
    {
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

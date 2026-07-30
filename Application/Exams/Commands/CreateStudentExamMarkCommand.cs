namespace Application.Exams.Commands
{
    public class CreateStudentExamMarkCommand
    {
        public Guid ExamId { get; set; }
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

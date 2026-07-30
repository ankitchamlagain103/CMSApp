namespace Domain.Entities
{
    // Detailed assessment scores for one student's sitting of one Exam. Hard-deleted (pure line
    // item, same convention as ClassSubject/TeacherAssignment/SalarySlipLine). Unique per
    // (ExamId, EnrollmentId).
    //
    // TotalMarks/IsAbsent are computed and persisted immediately on every create/update (obvious
    // for the admin to see right away), but Grade/GradePoint stay null until
    // ExamService.GenerateExamResultsAsync computes them in a batch against the current
    // GradeScale -- matching the design doc's workflow ordering ("Apply GradeScale" is step 9,
    // after "Lock Marks" step 6, not something that happens eagerly at entry time). Once
    // computed, Grade/GradePoint are a frozen snapshot -- editing GradeScale afterward doesn't
    // retroactively change an already-graded mark (see the Key Architectural Principle #2
    // reproducibility note in the design doc).
    public class StudentExamMark : AuditableEntity
    {
        public Guid Id { get; set; }
        public Guid ExamId { get; set; }
        public Guid EnrollmentId { get; set; }
        public decimal? TheoryObtainedMarks { get; set; }
        public decimal? PracticalObtainedMarks { get; set; }
        public decimal? InternalMarks { get; set; }
        public decimal TheoryGraceMarks { get; set; }
        public decimal PracticalGraceMarks { get; set; }
        public bool TheoryAbsent { get; set; }
        public bool PracticalAbsent { get; set; }
        public decimal TotalMarks { get; set; }
        public string Grade { get; set; }
        public decimal? GradePoint { get; set; }
        public string Remarks { get; set; }
        public bool IsAbsent { get; set; }
        public bool IsPublished { get; set; }

        public virtual Exam Exam { get; set; }
        public virtual Enrollment Enrollment { get; set; }
    }
}

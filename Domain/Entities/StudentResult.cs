using Domain.Enums;

namespace Domain.Entities
{
    // Aggregated final result for one enrollment within one exam term -- one row per
    // (EnrollmentId, ExamTermId), computed by ExamService.GenerateExamResultsAsync from the
    // enrollment's locked, marked StudentExamMark rows (final-exam schedules only -- see the
    // guide for why non-final exams like quizzes aren't blended in this pass). Soft-deleted like
    // Enrollment itself -- this is a real academic record, not a pure line item.
    //
    // Regenerating replaces this row's figures in place (same "regenerate rebuilds, doesn't
    // duplicate" convention as SalarySlip) EXCEPT when ResultStatus is already Withheld --
    // GenerateExamResultsAsync deliberately skips a Withheld row so a routine regenerate can't
    // silently clear an administrative hold; lifting the hold (removing the row) is a separate,
    // explicit action.
    //
    // Remarks is an addition beyond the source design doc's §4.2 table -- added so a Withheld
    // result (whose whole reason for existing is "administrative, fee, or disciplinary reasons",
    // per the doc's own §6.1 wording) has somewhere to record WHY.
    public class StudentResult : SoftDeleteAuditableEntity
    {
        public Guid Id { get; set; }
        public Guid EnrollmentId { get; set; }
        public Guid ExamTermId { get; set; }
        public decimal TotalMarks { get; set; }
        public decimal ObtainedMarks { get; set; }
        public decimal Percentage { get; set; }
        public decimal GPA { get; set; }
        public int? Rank { get; set; }
        public ResultStatus ResultStatus { get; set; }
        public DateTime? PublishedDate { get; set; }
        public string Remarks { get; set; }

        public virtual Enrollment Enrollment { get; set; }
        public virtual ExamTerm ExamTerm { get; set; }
    }
}

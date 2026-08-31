namespace Domain.Entities
{
    // Global grading schema (A+, A, B+, ...) with percentage bands and grade-point equivalents --
    // a real typed master-data table, not a Config catalog (MinPercent/MaxPercent/GradePoint are
    // structured facts a generic Code/Label/AdditionalValue row can't express cleanly, same
    // reasoning LeaveType/ClassSubject's grading columns already used). No FK ever points at this
    // table -- ExamService.GenerateExamResultsAsync looks up a row by percentage and COPIES its
    // Grade/GradePoint values onto the StudentExamMark row, so editing/deleting a GradeScale row
    // later never retroactively changes an already-computed result (see the reproducibility note
    // on StudentExamMark).
    public class GradeScale : SoftDeleteAuditableEntity
    {
        public Guid Id { get; set; }
        public string Grade { get; set; }
        public decimal MinPercent { get; set; }
        public decimal MaxPercent { get; set; }
        public decimal GradePoint { get; set; }
        public string Remarks { get; set; }
    }
}

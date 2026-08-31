using Domain.Enums;

namespace Domain.Entities
{
    // One qualification record per row (an employee holds many) -- 2026-07-23, moved here from a
    // Teacher-only TeacherQualification (renamed table dbo.employee_qualifications) so every
    // employee, not just teachers, can record qualifications. QualificationCode is a Config code
    // (ConfigTypeCodes.EmployeeQualification, e.g. BACHELORS/MASTERS), validated in the service
    // layer, not a database FK. Hard-deleted (child record of Employee).
    //
    // Verification (2026-08-07): same shape/rules as EmployeeDocument's own verification fields --
    // self-service-added rows start Pending, admin-route rows are auto-Approved.
    public class EmployeeQualification : AuditableEntity
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public string QualificationCode { get; set; }
        public string CourseName { get; set; }
        public string Institution { get; set; }
        public int? CompletionYear { get; set; }
        public string Score { get; set; }
        public string Remarks { get; set; }
        public VerificationStatus VerificationStatus { get; set; }
        public string VerificationRemarks { get; set; }
        public DateTimeOffset? VerifiedTs { get; set; }
        public string VerifiedBy { get; set; }
        public virtual Employee Employee { get; set; }
    }
}

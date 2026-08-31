using Domain.Enums;

namespace Application.Employees.Dtos
{
    public class EmployeeQualificationDto
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public string QualificationCode { get; set; }
        public string QualificationLabel { get; set; }
        public string CourseName { get; set; }
        public string Institution { get; set; }
        public int? CompletionYear { get; set; }
        public string Score { get; set; }
        public string Remarks { get; set; }
        public VerificationStatus VerificationStatus { get; set; }
        public string VerificationRemarks { get; set; }
        public DateTimeOffset? VerifiedTs { get; set; }
        public string VerifiedBy { get; set; }
    }
}

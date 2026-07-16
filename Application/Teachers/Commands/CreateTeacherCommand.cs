using Domain.Enums;

namespace Application.Teachers.Commands
{
    // Creates both the underlying Employee row (EmployeeCategory hardcoded Academic) and the
    // Teacher profile row (shared PK) in one call -- the common "hire a teacher" path.
    // JobPositionCode must be Teacher/Principal/Vice Principal (the same eligibility rule
    // EmployeeService.PromoteToTeacherAsync enforces for the standalone promotion path).
    public class CreateTeacherCommand
    {
        // Optional: leave null/blank and the backend generates the next EMP{year}{seq} number
        // (shared sequence across every employee type); supply a value only for records migrated
        // from an older system.
        public string EmployeeCode { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public Gender Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public DateTime? JoinDate { get; set; }
        public string JobPositionCode { get; set; }
        public string BankName { get; set; }
        public string BankAccountNumber { get; set; }
        public PaymentMode PaymentMode { get; set; }

        // Teacher-specific.
        public string TeachingLicenseNo { get; set; }
        public int? ExperienceYears { get; set; }
        public string Specialization { get; set; }
    }
}

using Domain.Enums;

namespace Application.Teachers.Dtos
{
    // Flattens both halves of the Employee/Teacher split: identity/HR fields come from Employee,
    // teaching fields from Teacher (same Id on both, via the shared-PK pattern).
    public class TeacherDto
    {
        public Guid Id { get; set; }
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
        public EmploymentStatus Status { get; set; }
        public string BankName { get; set; }
        public string BankAccountNumber { get; set; }
        public PaymentMode PaymentMode { get; set; }

        // Teacher-specific.
        public string TeachingLicenseNo { get; set; }
        public int? ExperienceYears { get; set; }
        public string Specialization { get; set; }

        // Detail endpoint only: assignments with their academic years, oldest first -- the
        // teacher's service history at this school. The paged list leaves it empty.
        public List<TeacherServiceHistoryDto> ServiceHistory { get; set; } = new List<TeacherServiceHistoryDto>();
    }
}

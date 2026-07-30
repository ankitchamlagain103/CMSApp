using Domain.Enums;

namespace Application.Employees.Dtos
{
    public class EmployeeDto
    {
        public Guid Id { get; set; }
        public Guid? UserId { get; set; }
        public string EmployeeCode { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public Gender Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public DateTime? JoinDate { get; set; }
        public string EmployeeCategoryCode { get; set; }
        public string JobPositionCode { get; set; }
        public EmploymentStatus EmploymentStatus { get; set; }
        public string BankName { get; set; }
        public string BankAccountNumber { get; set; }
        public PaymentMode PaymentMode { get; set; }

        // "Accounts and Codes" -- statutory/scheme identifiers, all optional (see the doc comment
        // on Domain/Entities/Employee.PanNumber).
        public string PanNumber { get; set; }
        public string ProvidentFundNumber { get; set; }
        public string SsfNumber { get; set; }
        public string CitNumber { get; set; }
        public string GratuityNumber { get; set; }

        public string BranchCode { get; set; }
        public string ProvinceCode { get; set; }
        public string LevelCode { get; set; }
        public Guid? ManagerId { get; set; }

        // Address chain (2026-07-24) -- see the doc comment on Domain/Entities/Employee.DistrictCode.
        public string DistrictCode { get; set; }
        public string LocalLevelCode { get; set; }
        public int? WardNo { get; set; }

        // Resolved only when the Manager navigation was loaded (GetByIdWithTeacherAsync); the
        // plain paged list leaves it null rather than issuing a per-row lookup, same convention
        // as HasTeacherProfile above.
        public string ManagerName { get; set; }

        // Whether a profile photo has been uploaded -- the photo bytes themselves are only ever
        // served through the dedicated download endpoint (PhotoPath is never exposed).
        public bool HasPhoto { get; set; }

        // Non-null only when this employee also has a Teacher profile (shared-PK 1:1).
        public bool HasTeacherProfile { get; set; }
        public string CreatedBy { get; set; }
        public DateTimeOffset CreatedTs { get; set; }
        public string UpdatedBy { get; set; }
        public DateTimeOffset? UpdatedTs { get; set; }
    }
}

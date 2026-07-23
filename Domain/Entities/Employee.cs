using Domain.Enums;

namespace Domain.Entities
{
    // The umbrella record for every staff member (teacher, principal, accountant, receptionist,
    // librarian, IT officer, driver, security guard, office assistant, cleaner, office help, ...).
    // Teacher (a thin teaching-specific profile) hangs off this via a SHARED primary key --
    // Teacher.Id == Employee.Id -- rather than Employee referencing Teacher, so
    // TeacherAssignment (which FKs to Teacher.Id) needed zero changes when this split was
    // introduced. Qualifications and Documents (2026-07-23) belong here directly, not to Teacher
    // -- neither concept is actually teaching-specific. EmployeeCategoryCode/JobPositionCode are
    // Config codes (ConfigTypeCodes.EmployeeCategory/JobPosition), validated in the service layer,
    // not database FKs -- same convention as every other Config-backed code column in this
    // codebase.
    public class Employee : SoftDeleteAuditableEntity
    {
        public Guid Id { get; set; }

        // Forward-looking only: no employee logins exist yet (same "records, not accounts" stance
        // as the pre-split Teacher/Student entities). Deliberately a plain nullable Guid, NOT a
        // navigation property -- Domain cannot reference ApplicationUser (Infrastructure/Identity),
        // per the RefreshToken placement rule. Unique when populated (partial index).
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

        // "Accounts and Codes" (2026-07-23) -- the statutory/scheme identifiers a payroll/HR
        // system needs on file per employee, distinct from the bank-payment fields above. All
        // free-form strings (no format validated -- PAN/PF/SSF/CIT/Gratuity numbering schemes
        // aren't standardized enough across employers to enforce a shape here) and all optional
        // (not every employee is enrolled in every scheme, e.g. Gratuity typically only vests
        // after a service-length threshold).
        public string PanNumber { get; set; }
        public string ProvidentFundNumber { get; set; }
        public string SsfNumber { get; set; }
        public string CitNumber { get; set; }
        public string GratuityNumber { get; set; }

        public virtual Teacher Teacher { get; set; }
        public virtual ICollection<EmployeeSalary> Salaries { get; set; } = new List<EmployeeSalary>();
        public virtual ICollection<EmployeeLoan> Loans { get; set; } = new List<EmployeeLoan>();
        public virtual ICollection<EmployeeQualification> Qualifications { get; set; } = new List<EmployeeQualification>();
        public virtual ICollection<EmployeeDocument> Documents { get; set; } = new List<EmployeeDocument>();
    }
}

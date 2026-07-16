using Domain.Enums;

namespace Domain.Entities
{
    // The umbrella record for every staff member (teacher, principal, accountant, receptionist,
    // librarian, IT officer, driver, security guard, office assistant, cleaner, office help, ...).
    // Teacher (a thin teaching-specific profile) hangs off this via a SHARED primary key --
    // Teacher.Id == Employee.Id -- rather than Employee referencing Teacher, so
    // TeacherAssignment/TeacherDocument/TeacherQualification (which FK to Teacher.Id) needed zero
    // changes when this split was introduced. EmployeeCategoryCode/JobPositionCode are Config
    // codes (ConfigTypeCodes.EmployeeCategory/JobPosition), validated in the service layer, not
    // database FKs -- same convention as every other Config-backed code column in this codebase.
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

        public virtual Teacher Teacher { get; set; }
        public virtual ICollection<EmployeeSalary> Salaries { get; set; } = new List<EmployeeSalary>();
        public virtual ICollection<EmployeeLoan> Loans { get; set; } = new List<EmployeeLoan>();
    }
}

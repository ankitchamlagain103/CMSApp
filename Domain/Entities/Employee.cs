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

        // "Org" fields for the Employee Profile page (2026-07-23). BranchCode/ProvinceCode/
        // LevelCode are Config codes (ConfigTypeCodes.Branch/Province/EmployeeLevel), same
        // validate-in-service-not-FK convention as EmployeeCategoryCode/JobPositionCode --
        // Designation on the profile UI is JobPositionCode's label, Department is
        // EmployeeCategoryCode's label, neither needed a new field. ManagerId is a real
        // self-referencing FK (Restrict, same reasoning as Menu's self-referencing ParentId) --
        // an employee's own record for "reporting manager", not a separate concept.
        public string BranchCode { get; set; }
        public string ProvinceCode { get; set; }
        public string LevelCode { get; set; }
        public Guid? ManagerId { get; set; }

        // Address chain (2026-07-24), extending ProvinceCode above into a full Nepal address:
        // Province -> District -> LocalLevel (municipality/rural municipality/metro/sub-metro)
        // -> WardNo. DistrictCode/LocalLevelCode are Config codes (ConfigTypeCodes.District/
        // LocalLevel), same validate-in-service-not-FK convention as every other Config-backed
        // column here. WardNo is a plain ward number (1-33 in practice, shape-only validated --
        // ward counts vary per local level and aren't tracked as catalog metadata). All three
        // optional; EmployeeService derives DistrictCode/ProvinceCode from LocalLevelCode
        // automatically when only the local level is supplied (see EmployeeService.ResolveAddressAsync).
        public string DistrictCode { get; set; }
        public string LocalLevelCode { get; set; }
        public int? WardNo { get; set; }

        // Storage-relative path (IFileStorageService handle), never a user-supplied path or a
        // publicly servable URL -- fetched via the same download-endpoint pattern as
        // EmployeeDocument, not exposed directly. Null = no photo uploaded yet.
        public string PhotoPath { get; set; }

        public virtual Teacher Teacher { get; set; }
        public virtual Employee Manager { get; set; }
        public virtual ICollection<EmployeeSalary> Salaries { get; set; } = new List<EmployeeSalary>();
        public virtual ICollection<EmployeeLoan> Loans { get; set; } = new List<EmployeeLoan>();
        public virtual ICollection<EmployeeQualification> Qualifications { get; set; } = new List<EmployeeQualification>();
        public virtual ICollection<EmployeeDocument> Documents { get; set; } = new List<EmployeeDocument>();
        public virtual ICollection<EmployeeLeaveBalance> LeaveBalances { get; set; } = new List<EmployeeLeaveBalance>();
        public virtual ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
    }
}

using Domain.Enums;

namespace Application.Employees.Commands
{
    // EmployeeCode optional -- blank = auto-generated EMP{year}{seq} (shared sequence across every
    // employee type, same helper Teacher/Student creation already use).
    public class CreateEmployeeCommand
    {
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

        // "Org" fields (2026-07-23) -- all optional. BranchCode/ProvinceCode/LevelCode are Config
        // catalog codes (1019/1020/1021); ManagerId is another Employee's id.
        public string BranchCode { get; set; }
        public string ProvinceCode { get; set; }
        public string LevelCode { get; set; }
        public Guid? ManagerId { get; set; }

        // Address chain (2026-07-24) -- all optional. DistrictCode/LocalLevelCode are Config
        // catalog codes (1022/1023); ProvinceCode/DistrictCode are auto-derived from
        // LocalLevelCode when left blank (see EmployeeService.ResolveAddressAsync).
        public string DistrictCode { get; set; }
        public string LocalLevelCode { get; set; }
        public int? WardNo { get; set; }

        // Teaching-specific fields (2026-08-06, ported from the removed standalone Teacher entity)
        // -- all optional, settable on any employee regardless of category/position.
        public string TeachingLicenseNo { get; set; }
        public int? ExperienceYears { get; set; }
        public string Specialization { get; set; }

        // Portal account provisioning (2026-07-27), on request -- when true, a login is created
        // for this employee in the same call (Email above must be set; RoleIds picks which role(s)
        // it gets, admin-chosen same as CreateUserCommand.RoleIds). False by default: creating an
        // Employee record never implies a login unless explicitly asked for.
        public bool RegisterUserAccount { get; set; }
        public List<Guid> RoleIds { get; set; } = new List<Guid>();
    }
}

namespace Application.Employees.Dtos
{
    // Composite response for the Employee Profile page -- one call instead of assembling it from
    // GetEmployeeByIdAsync + leave balances + leave requests + hand-computed dates, same
    // "one composite endpoint" pattern as GetTaxPlanningAsync/GetPayslipPreviewAsync.
    public class EmployeeProfileDto
    {
        public Guid Id { get; set; }
        public bool HasPhoto { get; set; }

        // Personal Information
        public string Name { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }

        // Employment Information -- Designation/Department resolve JobPositionCode/
        // EmployeeCategoryCode server-side (2026-08-05), same ConfigLabelHelper convention as
        // everywhere else in this codebase now uses.
        public string EmployeeCode { get; set; }
        public string LevelCode { get; set; }
        public string LevelLabel { get; set; }
        public string JobPositionCode { get; set; }
        public string JobPositionLabel { get; set; }
        public string EmployeeCategoryCode { get; set; }
        public string EmployeeCategoryLabel { get; set; }
        public string BranchCode { get; set; }
        public string BranchLabel { get; set; }
        public string ProvinceCode { get; set; }
        public string ProvinceLabel { get; set; }
        public DateTime? JoinDate { get; set; }

        // "3 years 4 months" -- computed from JoinDate against today, blank when JoinDate is null.
        public string ServicePeriod { get; set; }

        public Guid? ManagerId { get; set; }
        public string ManagerName { get; set; }

        public List<LeaveSummaryLineDto> LeaveSummary { get; set; } = new List<LeaveSummaryLineDto>();
        public List<UpcomingEventDto> UpcomingEvents { get; set; } = new List<UpcomingEventDto>();
        public List<LeaveRequestDto> PendingLeaveRequests { get; set; } = new List<LeaveRequestDto>();
    }
}

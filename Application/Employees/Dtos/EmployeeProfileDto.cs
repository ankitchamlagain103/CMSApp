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

        // Employment Information -- Designation/Department are JobPositionCode/
        // EmployeeCategoryCode's raw codes (same as everywhere else in this codebase, no
        // resolved-label convention exists for these two yet).
        public string EmployeeCode { get; set; }
        public string LevelCode { get; set; }
        public string JobPositionCode { get; set; }
        public string EmployeeCategoryCode { get; set; }
        public string BranchCode { get; set; }
        public string ProvinceCode { get; set; }
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

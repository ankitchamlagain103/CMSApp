namespace Application.Dashboard.Dtos
{
    // Composite "HR" dashboard widget (2026-07-28) -- one call covering headcount, pending
    // approvals, recent hires, and upcoming birthdays/anniversaries, the same one-call-per-persona
    // shape as DashboardSummaryDto.
    public class HrDashboardSummaryDto
    {
        public int TotalEmployees { get; set; }
        public List<EmploymentStatusCountDto> EmployeesByStatus { get; set; } = new List<EmploymentStatusCountDto>();
        public List<EmployeeCategoryCountDto> EmployeesByCategory { get; set; } = new List<EmployeeCategoryCountDto>();

        // HrStatus is the authoritative gate (per LeaveRequest's own design -- HR can decide
        // regardless of ManagerStatus), so this is the real "needs HR attention" count.
        public int PendingLeaveRequestCount { get; set; }
        public int PendingLoanRequestCount { get; set; }
        public List<RecentHireDto> RecentHires { get; set; } = new List<RecentHireDto>();

        // Next 30 days, rolled to next year once this year's date has passed (same logic
        // EmployeeService.GetEmployeeProfileAsync uses for a single employee).
        public List<UpcomingHrEventDto> UpcomingBirthdays { get; set; } = new List<UpcomingHrEventDto>();
        public List<UpcomingHrEventDto> UpcomingWorkAnniversaries { get; set; } = new List<UpcomingHrEventDto>();
    }
}

namespace Application.Employees.Dtos
{
    // Composite "My Dashboard" response for the Employee self-service landing page -- leave
    // status, class routine, and upcoming holidays/events in one call, same "one composite
    // endpoint" pattern as EmployeeProfileDto/GetTaxPlanningAsync/GetPayslipPreviewAsync.
    public class EmployeeDashboardDto
    {
        public Guid EmployeeId { get; set; }
        public string EmployeeName { get; set; }

        public List<LeaveSummaryLineDto> LeaveSummary { get; set; } = new List<LeaveSummaryLineDto>();
        public List<LeaveRequestDto> PendingLeaveRequests { get; set; } = new List<LeaveRequestDto>();

        // Every period this employee is assigned to teach, ordered by TimePeriodStartTime
        // (unset periods last, then by SubjectCode). This codebase has no day-of-week timetable
        // (see TeacherAssignment.TimePeriodId's own doc comment) -- a period recurs every school
        // day, so this is the employee's whole routine, not "today only".
        public List<TeacherAssignmentDto> ClassRoutine { get; set; } = new List<TeacherAssignmentDto>();

        // The routine entry whose period hasn't started yet today (Nepal time-of-day). Null once
        // every timed period for today has already started, or if the employee has no assignment
        // with a configured TimePeriod -- best-effort "next class" given the app has no per-day
        // schedule, see ClassRoutine's own doc comment.
        public TeacherAssignmentDto NextClass { get; set; }

        // Birthday/WorkAnniversary (live-computed, same figures as EmployeeProfileDto) merged
        // with upcoming school holidays/internal events/festivals from the shared Dual Calendar
        // module (Province/Branch-scoped events are filtered against this employee's own, both
        // null on the event meaning school-wide), sorted by date, capped to a dashboard-sized list.
        public List<UpcomingEventDto> UpcomingEvents { get; set; } = new List<UpcomingEventDto>();
    }
}

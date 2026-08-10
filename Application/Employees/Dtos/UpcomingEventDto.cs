namespace Application.Employees.Dtos
{
    // One row of the "Upcoming Events" widget on the Employee Profile and Dashboard pages.
    // "Birthday" / "WorkAnniversary" are computed live from Employee.DateOfBirth/JoinDate, not a
    // persisted row (see EmployeeService.BuildBirthdayAnniversaryEvents). The Dashboard
    // (GetEmployeeDashboardAsync) additionally merges in "Holiday" / "Event" / "Festival" rows
    // read from the shared Dual Calendar module.
    public class UpcomingEventDto
    {
        public string Type { get; set; }
        public string Label { get; set; }
        public DateTime Date { get; set; }
    }
}

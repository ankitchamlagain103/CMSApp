namespace Application.Employees.Dtos
{
    // One row of the Employee Profile's "Upcoming Events" widget -- computed live from
    // Employee.DateOfBirth/JoinDate (see EmployeeService.GetEmployeeProfileAsync), not a
    // persisted row. "Birthday" / "WorkAnniversary" are the only two Type values today.
    public class UpcomingEventDto
    {
        public string Type { get; set; }
        public string Label { get; set; }
        public DateTime Date { get; set; }
    }
}

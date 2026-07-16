using Domain.Enums;

namespace Application.Dashboard.Dtos
{
    public class DashboardTeacherSummaryDto
    {
        public Guid Id { get; set; }
        public string EmployeeNo { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public EmploymentStatus Status { get; set; }
        public DateTime? JoiningDate { get; set; }
    }
}

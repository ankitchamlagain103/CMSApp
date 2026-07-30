namespace Application.Employees.Dtos
{
    public class LeaveSubstituteDto
    {
        public Guid Id { get; set; }
        public Guid LeaveRequestId { get; set; }
        public Guid EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string Responsibility { get; set; }
    }
}

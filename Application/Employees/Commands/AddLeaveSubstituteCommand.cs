namespace Application.Employees.Commands
{
    public class AddLeaveSubstituteCommand
    {
        public Guid EmployeeId { get; set; }
        public string Responsibility { get; set; }
    }
}

namespace Application.Employees.Dtos
{
    public class BulkSalaryAdjustmentSkipDto
    {
        public Guid EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string Reason { get; set; }
    }
}

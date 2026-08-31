namespace Application.Employees.Dtos
{
    public class EmployeeLeaveBalanceDto
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public Guid LeaveTypeId { get; set; }
        public string LeaveTypeName { get; set; }
        public Guid FiscalYearId { get; set; }
        public string FiscalYearCode { get; set; }
        public decimal Allocated { get; set; }
        public decimal Used { get; set; }
        public decimal Pending { get; set; }
        public decimal Balance { get; set; }
    }
}

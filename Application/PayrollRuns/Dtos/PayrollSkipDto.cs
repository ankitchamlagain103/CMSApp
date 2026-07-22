namespace Application.PayrollRuns.Dtos
{
    public class PayrollSkipDto
    {
        public Guid EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string Reason { get; set; }
    }
}

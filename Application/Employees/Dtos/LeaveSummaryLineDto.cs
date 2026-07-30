namespace Application.Employees.Dtos
{
    // One row of the Employee Profile's "Leave Summary" widget -- Used/Allocated (e.g. "11/18").
    public class LeaveSummaryLineDto
    {
        public Guid LeaveTypeId { get; set; }
        public string LeaveTypeName { get; set; }
        public decimal Used { get; set; }
        public decimal Allocated { get; set; }
        public decimal Balance { get; set; }
    }
}

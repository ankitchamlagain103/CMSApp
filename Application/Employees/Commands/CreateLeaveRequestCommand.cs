namespace Application.Employees.Commands
{
    // The metadata half of "Apply Leave" -- the optional attachment file arrives as multipart
    // form data alongside this, same split as UploadEmployeeDocumentCommand.
    public class CreateLeaveRequestCommand
    {
        public Guid LeaveTypeId { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string Reason { get; set; }
        public Guid? SubstituteEmployeeId { get; set; }

        // 2026-07-24: the requester's own emergency claim -- see the doc comment on
        // Domain/Entities/LeaveRequest.IsEmergency for exactly what it does and doesn't do.
        public bool IsEmergency { get; set; }
    }
}

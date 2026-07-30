using Domain.Enums;

namespace Domain.Common.Filters
{
    public class LeaveRequestFilter
    {
        public Guid? EmployeeId { get; set; }
        public Guid? LeaveTypeId { get; set; }
        public LeaveApprovalStatus? ManagerStatus { get; set; }
        public LeaveApprovalStatus? HrStatus { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        // Requests where at least one of ManagerStatus/HrStatus is still Pending -- the "Pending
        // Requests" widget on the Employee Profile page and the manager/HR review queues.
        public bool? IsPending { get; set; }
    }
}

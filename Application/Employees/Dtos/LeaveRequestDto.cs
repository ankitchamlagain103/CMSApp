using Domain.Enums;

namespace Application.Employees.Dtos
{
    public class LeaveRequestDto
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public Guid LeaveTypeId { get; set; }
        public string LeaveTypeName { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal Days { get; set; }
        public string Reason { get; set; }
        public Guid? SubstituteEmployeeId { get; set; }
        public string SubstituteEmployeeName { get; set; }
        public bool IsEmergency { get; set; }

        public LeaveApprovalStatus ManagerStatus { get; set; }
        public string ManagerRemarks { get; set; }
        public DateTimeOffset? ManagerDecisionTs { get; set; }
        public string ManagerDecisionBy { get; set; }

        public LeaveApprovalStatus HrStatus { get; set; }
        public string HrRemarks { get; set; }
        public DateTimeOffset? HrDecisionTs { get; set; }
        public string HrDecisionBy { get; set; }

        // Derived, not persisted -- see LeaveRequest's own doc comment for the exact derivation
        // rule (HrStatus is authoritative; a Pending HrStatus with a Rejected ManagerStatus shows
        // as a provisional Rejected that HR can still override).
        public LeaveApprovalStatus EffectiveStatus { get; set; }

        public bool HasAttachment { get; set; }
        public string AttachmentFileName { get; set; }

        public List<LeaveSubstituteDto> Substitutes { get; set; } = new List<LeaveSubstituteDto>();
    }
}

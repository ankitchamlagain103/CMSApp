using Domain.Enums;

namespace Domain.Entities
{
    // A leave application. Two independent reviewer fields, not one status column:
    // ManagerStatus (the requester's direct manager, resolved via Employee.ManagerId) and
    // HrStatus (HR staff, permission-gated) -- each is a one-shot Pending -> Approved/Rejected
    // transition (LeaveRequestService rejects a second decision on an already-decided field,
    // same "Pending-only" convention as SalaryAdjustment/FeeAdjustment/EmployeeLoan).
    //
    // Deliberate design: the normal flow is Manager decides first, then HR -- but HR is
    // authoritative and is NOT gated on ManagerStatus at all (per explicit instruction: "in
    // emergency cases HR should be able to directly approve"). HrStatus alone drives whether
    // leave is actually granted (LeaveRequestService.RecordHrDecisionAsync deducts the balance
    // only when HrStatus flips to Approved) -- HR can approve even if the manager already
    // rejected, or before the manager has acted at all. LeaveRequestService derives an
    // "effective" status for display (see LeaveRequestMapper) rather than persisting a third
    // overall-status column, so it can never drift out of sync with the two source fields:
    // HrStatus == Rejected -> Rejected; HrStatus == Approved -> Approved; HrStatus == Pending
    // and ManagerStatus == Rejected -> Rejected (provisional -- HR can still override by
    // approving); otherwise Pending.
    //
    // SubstituteEmployeeId is the quick single "who's covering" reference the Apply
    // Leave/Assign Substitute UI sets directly; LeaveSubstitutes (the child collection) is the
    // fuller multi-person breakdown with a per-person Responsibility, for when more than one
    // colleague splits the requester's duties. Soft-deleted -- an HR-audit financial/attendance
    // record, same tier as StudentDiscount/EmployeeLoan.
    public class LeaveRequest : SoftDeleteAuditableEntity
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public Guid LeaveTypeId { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal Days { get; set; }
        public string Reason { get; set; }
        public Guid? SubstituteEmployeeId { get; set; }

        // 2026-07-24: marks a request as an emergency application -- the requester's own claim,
        // not independently verified. Its only effect is in EmployeeService.ValidateLeavePolicyAsync:
        // a request flagged IsEmergency unconditionally bypasses its leave type's
        // MaxConsecutiveDays/MaxDaysPerWeek/MaxDaysPerMonth caps (no per-type toggle -- whether a
        // supporting document backs the request is left to the requester/HR conversation, not a
        // system gate). Reviewers see this flag on the request and can weigh it during
        // manager/HR decision -- it does not auto-approve anything.
        public bool IsEmergency { get; set; }

        public LeaveApprovalStatus ManagerStatus { get; set; }
        public string ManagerRemarks { get; set; }
        public DateTimeOffset? ManagerDecisionTs { get; set; }
        public string ManagerDecisionBy { get; set; }

        public LeaveApprovalStatus HrStatus { get; set; }
        public string HrRemarks { get; set; }
        public DateTimeOffset? HrDecisionTs { get; set; }
        public string HrDecisionBy { get; set; }

        // Single-file attachment (supporting document, e.g. a medical certificate) -- same
        // IFileStorageService handle convention as EmployeeDocument/StudentDocument, just inlined
        // on the request itself instead of a child table, since a leave request only ever needs
        // at most one.
        public string AttachmentPath { get; set; }
        public string AttachmentFileName { get; set; }
        public string AttachmentContentType { get; set; }

        public virtual Employee Employee { get; set; }
        public virtual LeaveType LeaveType { get; set; }
        public virtual Employee SubstituteEmployee { get; set; }
        public virtual ICollection<LeaveSubstitute> Substitutes { get; set; } = new List<LeaveSubstitute>();
    }
}

namespace Domain.Entities
{
    // A named leave category an institution offers (Annual, Sick, Casual, ...) -- real typed
    // columns rather than a Config catalog entry, deliberately: DaysPerYear/CarryForward/IsPaid
    // are structured facts a generic Code/Label/3-AdditionalValue Config row can't express
    // cleanly, same reasoning ClassSubject's grading columns used over stuffing them into
    // Config.AdditionalValue1. Soft-deleted -- referenced by EmployeeLeaveBalance/LeaveRequest,
    // so an in-use type can't just vanish (see LeaveTypeService.DeleteLeaveTypeAsync's Conflict
    // guard).
    public class LeaveType : SoftDeleteAuditableEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public decimal DaysPerYear { get; set; }
        public bool CarryForward { get; set; }
        public bool IsPaid { get; set; }

        // Policy configuration (2026-07-24) -- both nullable/optional, so an unconfigured leave
        // type behaves exactly as before (no caps). Enforced in
        // EmployeeService.CreateLeaveRequestAsync (see ValidateLeavePolicyAsync), not here --
        // same "entity is data, service is behavior" convention as the rest of this codebase.
        // MaxConsecutiveDays: the most days a single request of this type may span in one go.
        // MaxDaysPerWeek/MaxDaysPerMonth: the most days of this type an employee may have
        // Pending+Approved within the calendar week/month the request's FromDate falls in.
        // (A per-type "requires a document above N days" / "allow emergency override" pair was
        // considered and deliberately dropped: whether a supporting document is provided is left
        // to the requester/HR conversation at apply/approval time rather than a hard system gate
        // -- LeaveRequest.AttachmentPath stays purely optional for every leave type. A request
        // flagged LeaveRequest.IsEmergency unconditionally bypasses the two caps below for any
        // leave type -- no per-type toggle needed once the document gate was removed.)
        public int? MaxConsecutiveDays { get; set; }
        public decimal? MaxDaysPerWeek { get; set; }
        public decimal? MaxDaysPerMonth { get; set; }
    }
}

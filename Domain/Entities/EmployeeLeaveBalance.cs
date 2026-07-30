namespace Domain.Entities
{
    // One employee's running leave balance for one LeaveType in one FiscalYear -- scoped by
    // fiscal year (a deliberate addition beyond the raw sketch this was built from, which had no
    // year field at all: an unscoped "allocated 18 days" is meaningless once the year rolls over
    // and the balance needs to reset, and this codebase already has a FiscalYear entity built
    // exactly for this kind of periodic reset, reused here rather than inventing a parallel
    // "LeaveYear" concept). Balance is a real maintained column (Allocated - Used - Pending),
    // recalculated by LeaveRequestService on every mutation that touches Used/Pending -- not
    // computed at read time, since the sketch that requested this schema listed it as a persisted
    // column. Hard-deleted: a balance row is allocated-then-adjusted, never meaningfully
    // "deleted" through the API (no delete endpoint is exposed).
    public class EmployeeLeaveBalance : AuditableEntity
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public Guid LeaveTypeId { get; set; }
        public Guid FiscalYearId { get; set; }
        public decimal Allocated { get; set; }
        public decimal Used { get; set; }
        public decimal Pending { get; set; }
        public decimal Balance { get; set; }

        public virtual Employee Employee { get; set; }
        public virtual LeaveType LeaveType { get; set; }
        public virtual FiscalYear FiscalYear { get; set; }
    }
}

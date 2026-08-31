namespace Domain.Entities
{
    // One colleague covering part of a leave requester's duties, with what they're covering.
    // Pure link/child row (like StudentGuardian/EnrollmentSubject) -- hard-deleted.
    public class LeaveSubstitute : AuditableEntity
    {
        public Guid Id { get; set; }
        public Guid LeaveRequestId { get; set; }
        public Guid EmployeeId { get; set; }
        public string Responsibility { get; set; }

        public virtual LeaveRequest LeaveRequest { get; set; }
        public virtual Employee Employee { get; set; }
    }
}

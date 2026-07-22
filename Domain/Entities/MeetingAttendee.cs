using Domain.Enums;

namespace Domain.Entities
{
    // One invitee on one meeting, keyed by email (invitees don't have to be system users, so
    // UserId is nullable and uniqueness is per (MeetingId, Email) -- an all-empty-Guid UserId
    // pair could never be unique). Pure child/link row -- hard-deleted AuditableEntity, owned
    // by IMeetingRepository, same convention as EnrollmentSubject/StudentGuardian.
    public class MeetingAttendee : AuditableEntity
    {
        public Guid Id { get; set; }
        public Guid MeetingId { get; set; }
        public Guid? UserId { get; set; }
        public string Email { get; set; }
        public AttendeeStatus Status { get; set; } = AttendeeStatus.Pending;

        public virtual Meeting Meeting { get; set; }
    }
}

namespace Domain.Entities
{
    // A scheduled meeting on one calendar day. AdDate is canonical (date column);
    // BsYear/BsMonth/BsDay are denormalized via IBsAdConversionService on save, whichever
    // calendar the scheduler used. HostUserId is a plain scalar (the Identity user id), NOT a
    // navigation property -- Domain can't reference ApplicationUser, same rule that keeps
    // RefreshToken out of Domain. Soft-deleted: deleting a meeting is "cancelled", and the
    // row stays for audit.
    public class Meeting : SoftDeleteAuditableEntity
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }

        public DateTime AdDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        public int BsYear { get; set; }
        public int BsMonth { get; set; }
        public int BsDay { get; set; }

        public bool IsVirtual { get; set; }

        // Physical room name or virtual URL (Zoom/Teams/Meet).
        public string Location { get; set; }

        public Guid HostUserId { get; set; }

        public virtual ICollection<MeetingAttendee> Attendees { get; set; } = new List<MeetingAttendee>();
    }
}

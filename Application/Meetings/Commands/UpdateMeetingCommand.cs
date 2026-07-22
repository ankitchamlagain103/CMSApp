namespace Application.Meetings.Commands
{
    // Same dual-calendar date semantics as ScheduleMeetingCommand. AttendeeEmails has
    // three-way semantics mirroring UpdateUserCommand.RoleIds: null = leave the attendee list
    // unchanged, [] = remove everyone, non-empty = replace-sync (existing attendees keep
    // their RSVP status, new emails join as Pending, missing emails are removed). The host
    // is immutable on update.
    public class UpdateMeetingCommand
    {
        public string Title { get; set; }
        public string Description { get; set; }

        public bool IsBsScheduled { get; set; }
        public DateTime? ScheduledAdDate { get; set; }
        public int? ScheduledBsYear { get; set; }
        public int? ScheduledBsMonth { get; set; }
        public int? ScheduledBsDay { get; set; }

        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        public bool IsVirtual { get; set; }
        public string Location { get; set; }

        public List<string> AttendeeEmails { get; set; }
    }
}

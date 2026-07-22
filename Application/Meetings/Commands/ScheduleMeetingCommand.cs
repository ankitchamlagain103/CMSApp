namespace Application.Meetings.Commands
{
    // The meeting date may be given in either calendar: IsBsScheduled=false -> ScheduledAdDate
    // is required; IsBsScheduled=true -> ScheduledBsYear/Month/Day are required. The service
    // computes and stores the other calendar's fields. HostUserId is optional -- when omitted
    // the authenticated caller becomes the host.
    public class ScheduleMeetingCommand
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

        public Guid? HostUserId { get; set; }
        public List<string> AttendeeEmails { get; set; } = new List<string>();
    }
}

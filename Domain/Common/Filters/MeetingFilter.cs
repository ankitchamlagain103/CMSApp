namespace Domain.Common.Filters
{
    public class MeetingFilter
    {
        public DateTime? FromAdDate { get; set; }
        public DateTime? ToAdDate { get; set; }
        public Guid? HostUserId { get; set; }
    }
}

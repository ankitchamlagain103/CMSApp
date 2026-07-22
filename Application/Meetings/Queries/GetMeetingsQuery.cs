namespace Application.Meetings.Queries
{
    public class GetMeetingsQuery
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public DateTime? FromAdDate { get; set; }
        public DateTime? ToAdDate { get; set; }
        public Guid? HostUserId { get; set; }
    }
}

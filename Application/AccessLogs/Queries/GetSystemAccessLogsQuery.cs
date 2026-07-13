namespace Application.AccessLogs.Queries
{
    public class GetSystemAccessLogsQuery
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public Guid? UserId { get; set; }
    }
}

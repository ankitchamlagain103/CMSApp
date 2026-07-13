namespace Application.AccessLogs.Dtos
{
    public class SystemAccessLogDto
    {
        public long Id { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; }
        public string Controller { get; set; }
        public string Action { get; set; }
        public string HttpMethod { get; set; }
        public string Url { get; set; }
        public string IpAddress { get; set; }
        public DateTimeOffset AccessedTs { get; set; }
    }
}

namespace Application.AccessLogs.Commands
{
    public class CreateSystemAccessLogCommand
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; }
        public string Controller { get; set; }
        public string Action { get; set; }
        public string HttpMethod { get; set; }
        public string Url { get; set; }
        public string IpAddress { get; set; }
    }
}

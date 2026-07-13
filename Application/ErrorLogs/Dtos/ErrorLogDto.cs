namespace Application.ErrorLogs.Dtos
{
    public class ErrorLogDto
    {
        public long Id { get; set; }
        public string ExceptionType { get; set; }
        public string Message { get; set; }
        public string Path { get; set; }
        public int ErrorCount { get; set; }
        public DateTimeOffset FirstOccurredTs { get; set; }
        public DateTimeOffset LastOccurredTs { get; set; }
    }
}

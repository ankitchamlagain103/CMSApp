namespace Domain.Entities
{
    public class ErrorLog : AuditableEntity
    {
        public long Id { get; set; }
        public string FingerprintHash { get; set; }
        public string ExceptionType { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
        public string Path { get; set; }
        public int ErrorCount { get; set; }
        public DateTimeOffset LastOccurredTs { get; set; }
    }
}

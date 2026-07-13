namespace Application.ErrorLogs.Commands
{
    public class CreateErrorLogCommand
    {
        public string ExceptionType { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
        public string Path { get; set; }
    }
}

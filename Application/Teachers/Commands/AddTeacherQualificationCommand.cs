namespace Application.Teachers.Commands
{
    public class AddTeacherQualificationCommand
    {
        public string QualificationCode { get; set; }
        public string CourseName { get; set; }
        public string Institution { get; set; }
        public int? CompletionYear { get; set; }
        public string Score { get; set; }
        public string Remarks { get; set; }
    }
}

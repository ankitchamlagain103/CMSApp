namespace Application.Teachers.Dtos
{
    public class TeacherQualificationDto
    {
        public Guid Id { get; set; }
        public Guid TeacherId { get; set; }
        public string QualificationCode { get; set; }
        public string CourseName { get; set; }
        public string Institution { get; set; }
        public int? CompletionYear { get; set; }
        public string Score { get; set; }
        public string Remarks { get; set; }
    }
}

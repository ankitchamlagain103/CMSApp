namespace Application.Exams.Dtos
{
    public class ExamRoutineSkipDto
    {
        public Guid ClassSubjectId { get; set; }
        public string SubjectCode { get; set; }
        public string Reason { get; set; }
    }
}

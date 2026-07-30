namespace Application.Exams.Dtos
{
    public class CreateExamRoutineResultDto
    {
        public Guid ExamTermId { get; set; }
        public Guid AcademicClassId { get; set; }
        public List<ExamDto> Created { get; set; } = new List<ExamDto>();
        public List<ExamRoutineSkipDto> Skipped { get; set; } = new List<ExamRoutineSkipDto>();
    }
}

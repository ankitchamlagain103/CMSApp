namespace Application.Exams.Dtos
{
    public class GenerateExamResultsResultDto
    {
        public Guid ExamTermId { get; set; }
        public int GeneratedCount { get; set; }
        public List<ExamResultGenerationSkipDto> Skipped { get; set; } = new List<ExamResultGenerationSkipDto>();
    }
}

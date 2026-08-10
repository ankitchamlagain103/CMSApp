namespace Application.Exams.Dtos
{
    // One subject's contribution to a StudentResult -- sourced from the enrollment's final-exam
    // StudentExamMark for that subject (see ExamService.GenerateExamResultsAsync for why only the
    // final exam feeds this, not every exam scheduled for the subject that term).
    public class ExamResultSubjectDto
    {
        public Guid ClassSubjectId { get; set; }
        public string SubjectCode { get; set; }
        public string SubjectLabel { get; set; }
        public int? FullMarks { get; set; }
        public int? PassMarks { get; set; }
        public decimal ObtainedMarks { get; set; }
        public string Grade { get; set; }
        public decimal? GradePoint { get; set; }
        public bool IsAbsent { get; set; }
        public bool Passed { get; set; }
    }
}

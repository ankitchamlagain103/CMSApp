using Domain.Enums;

namespace Application.Exams.Dtos
{
    // StudentName/AdmissionNo/GradeCode/SectionCode are flattened from the enrollment chain, same
    // convention as EnrollmentDto -- a results table renders without a lookup per row.
    public class StudentResultDto
    {
        public Guid Id { get; set; }
        public Guid EnrollmentId { get; set; }
        public string StudentName { get; set; }
        public string AdmissionNo { get; set; }
        public string GradeCode { get; set; }
        public string GradeLabel { get; set; }
        public string SectionCode { get; set; }
        public string SectionLabel { get; set; }
        public Guid ExamTermId { get; set; }
        public decimal TotalMarks { get; set; }
        public decimal ObtainedMarks { get; set; }
        public decimal Percentage { get; set; }
        public decimal GPA { get; set; }
        public int? Rank { get; set; }
        public ResultStatus ResultStatus { get; set; }
        public DateTime? PublishedDate { get; set; }
        public string Remarks { get; set; }
    }
}

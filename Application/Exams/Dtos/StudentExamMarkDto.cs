namespace Application.Exams.Dtos
{
    // SubjectCode/StudentName/AdmissionNo are flattened in from navigations, same convention as
    // ExamScheduleDto's SubjectCode/SectionCode.
    public class StudentExamMarkDto
    {
        public Guid Id { get; set; }
        public Guid ExamId { get; set; }
        public string SubjectCode { get; set; }
        public string SubjectLabel { get; set; }
        public Guid EnrollmentId { get; set; }
        public string StudentName { get; set; }
        public string AdmissionNo { get; set; }
        public decimal? TheoryObtainedMarks { get; set; }
        public decimal? PracticalObtainedMarks { get; set; }
        public decimal? InternalMarks { get; set; }
        public decimal TheoryGraceMarks { get; set; }
        public decimal PracticalGraceMarks { get; set; }
        public bool TheoryAbsent { get; set; }
        public bool PracticalAbsent { get; set; }
        public decimal TotalMarks { get; set; }
        public string Grade { get; set; }
        public decimal? GradePoint { get; set; }
        public string Remarks { get; set; }
        public bool IsAbsent { get; set; }
        public bool IsPublished { get; set; }
    }
}

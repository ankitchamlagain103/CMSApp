namespace Application.Students.Dtos
{
    // The student profile's "current class" block: the active enrollment (preferring the
    // IsCurrent academic year) with class/section/year flattened in and the subjects the
    // student is studying. Grade/section/subject codes resolve to labels from the UI's cached
    // dropdown data.
    public class StudentCurrentEnrollmentDto
    {
        public Guid EnrollmentId { get; set; }
        public Guid AcademicYearId { get; set; }
        public string AcademicYearCode { get; set; }
        public string AcademicYearName { get; set; }
        public Guid AcademicClassId { get; set; }
        public string GradeCode { get; set; }
        public Guid ClassSectionId { get; set; }
        public string SectionCode { get; set; }
        public string RollNumber { get; set; }
        public DateTime? EnrollmentDate { get; set; }
        public List<StudentSubjectDto> Subjects { get; set; } = new List<StudentSubjectDto>();
    }
}

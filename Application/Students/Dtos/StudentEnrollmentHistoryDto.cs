using Domain.Enums;

namespace Application.Students.Dtos
{
    // One row of the student's schooling history -- every enrollment ever, any status, ordered
    // oldest year first. The first row answers "studying in this school since which year"; the
    // Status values tell the promotion/transfer story year by year.
    public class StudentEnrollmentHistoryDto
    {
        public Guid EnrollmentId { get; set; }
        public Guid AcademicYearId { get; set; }
        public string AcademicYearCode { get; set; }
        public string AcademicYearName { get; set; }
        public DateTime AcademicYearStartDate { get; set; }
        public Guid AcademicClassId { get; set; }
        public string GradeCode { get; set; }
        public Guid ClassSectionId { get; set; }
        public string SectionCode { get; set; }
        public string RollNumber { get; set; }
        public DateTime? EnrollmentDate { get; set; }
        public EnrollmentStatus Status { get; set; }
    }
}

using Domain.Enums;

namespace Application.Students.Dtos
{
    // One row of the student's schooling history -- every enrollment ever, any status, ordered
    // oldest year first. The first row answers "studying in this school since which year"; the
    // Status values tell the promotion/transfer story year by year. Served by its own
    // GET /api/students/{id}/enrollment-history (2026-08-05) -- no longer embedded in the main
    // student GET, since it's the "History" tab's data, not something every profile load needs.
    public class StudentEnrollmentHistoryDto
    {
        public Guid EnrollmentId { get; set; }
        public Guid AcademicYearId { get; set; }
        public string AcademicYearCode { get; set; }
        public string AcademicYearName { get; set; }
        public DateTime AcademicYearStartDate { get; set; }
        public Guid AcademicClassId { get; set; }
        public string GradeCode { get; set; }
        public string GradeLabel { get; set; }
        public Guid ClassSectionId { get; set; }
        public string SectionCode { get; set; }
        public string SectionLabel { get; set; }
        public string RollNumber { get; set; }
        public DateTime? EnrollmentDate { get; set; }
        public EnrollmentStatus Status { get; set; }
    }
}

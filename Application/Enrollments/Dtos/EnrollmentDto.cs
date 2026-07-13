using Domain.Enums;

namespace Application.Enrollments.Dtos
{
    // Student and class/section info are flattened in so the roster screen doesn't need one
    // extra call per row. GradeCode/SectionCode are Config codes -- the UI resolves labels from
    // its cached dropdown data.
    public class EnrollmentDto
    {
        public Guid Id { get; set; }
        public Guid StudentId { get; set; }
        public Guid ClassSectionId { get; set; }
        public Guid AcademicClassId { get; set; }
        public Guid AcademicYearId { get; set; }
        public string GradeCode { get; set; }
        public string SectionCode { get; set; }
        public string RollNumber { get; set; }
        public DateTime? EnrollmentDate { get; set; }
        public EnrollmentStatus Status { get; set; }
        public string StudentAdmissionNo { get; set; }
        public string StudentFirstName { get; set; }
        public string StudentLastName { get; set; }
    }
}

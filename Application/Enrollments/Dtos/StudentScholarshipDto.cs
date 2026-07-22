using Domain.Enums;

namespace Application.Enrollments.Dtos
{
    public class StudentScholarshipDto
    {
        public Guid Id { get; set; }
        public Guid EnrollmentId { get; set; }
        public string ScholarshipTypeCode { get; set; }

        // Human-readable ScholarshipType catalog label (2026-07-19); falls back to the code
        // when the option no longer exists in the catalog.
        public string ScholarshipTypeLabel { get; set; }
        public AwardValueType ValueType { get; set; }
        public decimal Value { get; set; }
        public string Remarks { get; set; }
    }
}

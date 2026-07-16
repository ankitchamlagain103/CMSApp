using Domain.Enums;

namespace Application.Enrollments.Dtos
{
    public class StudentScholarshipDto
    {
        public Guid Id { get; set; }
        public Guid EnrollmentId { get; set; }
        public string ScholarshipTypeCode { get; set; }
        public AwardValueType ValueType { get; set; }
        public decimal Value { get; set; }
        public string Remarks { get; set; }
    }
}

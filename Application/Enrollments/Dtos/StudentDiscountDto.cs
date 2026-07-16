using Domain.Enums;

namespace Application.Enrollments.Dtos
{
    public class StudentDiscountDto
    {
        public Guid Id { get; set; }
        public Guid EnrollmentId { get; set; }
        public string DiscountTypeCode { get; set; }
        public AwardValueType ValueType { get; set; }
        public decimal Value { get; set; }
        public string Remarks { get; set; }
    }
}

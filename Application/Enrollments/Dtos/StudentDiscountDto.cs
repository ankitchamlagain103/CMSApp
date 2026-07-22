using Domain.Enums;

namespace Application.Enrollments.Dtos
{
    public class StudentDiscountDto
    {
        public Guid Id { get; set; }
        public Guid EnrollmentId { get; set; }
        public string DiscountTypeCode { get; set; }

        // Human-readable DiscountType catalog label (2026-07-19); falls back to the code when
        // the option no longer exists in the catalog.
        public string DiscountTypeLabel { get; set; }
        public AwardValueType ValueType { get; set; }
        public decimal Value { get; set; }
        public string Remarks { get; set; }
    }
}

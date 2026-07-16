using Domain.Enums;

namespace Domain.Entities
{
    // A discount awarded against one Enrollment (student + section + class + year already
    // resolved through it). DiscountTypeCode is a Config code (ConfigTypeCodes.DiscountType),
    // validated in the service layer, not a database FK -- same convention as SubjectCode/
    // GradeCode. Soft-deleted (not hard, unlike ClassSubject/TeacherAssignment): this is a
    // financial-audit record, so an accidental removal must still leave a trace.
    public class StudentDiscount : SoftDeleteAuditableEntity
    {
        public Guid Id { get; set; }
        public Guid EnrollmentId { get; set; }
        public string DiscountTypeCode { get; set; }
        public AwardValueType ValueType { get; set; }
        public decimal Value { get; set; }
        public string Remarks { get; set; }
        public virtual Enrollment Enrollment { get; set; }
    }
}

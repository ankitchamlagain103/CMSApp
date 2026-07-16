using Domain.Enums;

namespace Domain.Entities
{
    // A scholarship awarded against one Enrollment -- same shape and reasoning as StudentDiscount,
    // kept as a separate entity/table (rather than one polymorphic "award" type) because
    // scholarships and discounts are reported on separately (e.g. "how many students hold a
    // scholarship this year" is a scholarship-only question) and use distinct Config catalogs.
    // ScholarshipTypeCode is a Config code (ConfigTypeCodes.ScholarshipType) -- this is the
    // "configurable criteria" (class topper, exam merit, social category, ...): admin-extensible
    // via POST /api/configs, no hardcoded list. Soft-deleted for the same audit reason as
    // StudentDiscount.
    public class StudentScholarship : SoftDeleteAuditableEntity
    {
        public Guid Id { get; set; }
        public Guid EnrollmentId { get; set; }
        public string ScholarshipTypeCode { get; set; }
        public AwardValueType ValueType { get; set; }
        public decimal Value { get; set; }
        public string Remarks { get; set; }
        public virtual Enrollment Enrollment { get; set; }
    }
}

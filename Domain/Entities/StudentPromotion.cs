using Domain.Enums;

namespace Domain.Entities
{
    // Immutable audit record of a student's movement between enrollments -- promotion (next
    // year, next grade), retention (next year, same grade), or a mid-year transfer. Per the
    // design doc's "Core Architecture: Immutability Principles", an Enrollment is never edited
    // to reflect progression; PromotionService always creates a brand-new Enrollment (via
    // IEnrollmentService, so it gets the same capacity/roll-number/one-active-enrollment
    // validation every other enrollment does) and logs the transition here. AuditableEntity
    // (hard, no soft-delete flag) -- this is a pure audit trail, there is no "undo a promotion"
    // operation, only creating further rows (e.g. a later transfer).
    public class StudentPromotion : AuditableEntity
    {
        public Guid Id { get; set; }
        public Guid StudentId { get; set; }
        public Guid FromEnrollmentId { get; set; }
        public Guid ToEnrollmentId { get; set; }
        public DateTime PromotionDate { get; set; }
        public PromotionType PromotionType { get; set; }
        public string Remarks { get; set; }

        public virtual Student Student { get; set; }
        public virtual Enrollment FromEnrollment { get; set; }
        public virtual Enrollment ToEnrollment { get; set; }
    }
}

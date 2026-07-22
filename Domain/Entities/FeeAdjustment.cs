using Domain.Enums;

namespace Domain.Entities
{
    // A pre-generation monthly fee override for one enrollment ("special discount this March",
    // "damaged-book charge in June") -- the middle tier between global recurring configuration
    // (FeeStructureItem/StudentDiscount) and one-time manual edits on a Draft invoice.
    // AdjustmentTypeCode is a Config code (ConfigTypeCodes.FeeAdjustmentType), validated in the
    // service layer, not a database FK. Pending rows are consumed by the generation of their
    // (BillingYear, BillingMonth) and stamped Applied + AppliedFeeInvoiceId; regeneration of a
    // Draft invoice re-pends them. Soft-deleted (financial-audit record).
    public class FeeAdjustment : SoftDeleteAuditableEntity
    {
        public Guid Id { get; set; }
        public Guid EnrollmentId { get; set; }
        public int BillingYear { get; set; }
        public int BillingMonth { get; set; }
        public string AdjustmentTypeCode { get; set; }

        // Optional (2026-07-17): scopes a Percentage adjustment to one FeeCategory's recurring
        // subtotal instead of the whole invoice's -- same convention as FeeRule.FeeCategoryCode.
        // Null = whole-invoice recurring subtotal (the original behavior). Always informational
        // for a FixedAmount adjustment (helps categorize/report it) since a flat amount doesn't
        // need a base to resolve against.
        public string FeeCategoryCode { get; set; }
        public AdjustmentDirection Direction { get; set; }
        public AwardValueType ValueType { get; set; }
        public decimal Value { get; set; }
        public string Remarks { get; set; }
        public AdjustmentStatus Status { get; set; }
        public Guid? AppliedFeeInvoiceId { get; set; }

        public virtual Enrollment Enrollment { get; set; }
    }
}

using Domain.Enums;

namespace Application.FeeInvoices.Dtos
{
    public class FeeAdjustmentDto
    {
        public Guid Id { get; set; }
        public Guid EnrollmentId { get; set; }
        public string StudentName { get; set; }
        public string AdmissionNo { get; set; }
        public string GradeCode { get; set; }
        public string SectionCode { get; set; }
        public int BillingYear { get; set; }
        public int BillingMonth { get; set; }
        public string AdjustmentTypeCode { get; set; }

        // Human-readable catalog labels (2026-07-19) -- FeeAdjustmentType (1017) and
        // FeeCategory (1010); each falls back to its code when the option no longer exists.
        public string AdjustmentTypeLabel { get; set; }
        public string FeeCategoryCode { get; set; }
        public string FeeCategoryLabel { get; set; }
        public AdjustmentDirection Direction { get; set; }
        public AwardValueType ValueType { get; set; }
        public decimal Value { get; set; }
        public string Remarks { get; set; }
        public AdjustmentStatus Status { get; set; }
        public Guid? AppliedFeeInvoiceId { get; set; }
    }
}

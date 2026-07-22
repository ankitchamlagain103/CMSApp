using Domain.Enums;

namespace Application.FeeInvoices.Commands
{
    public class CreateFeeAdjustmentCommand
    {
        public Guid EnrollmentId { get; set; }
        public int BillingYear { get; set; }
        public int BillingMonth { get; set; }
        public string AdjustmentTypeCode { get; set; }

        // Optional (2026-07-17): scopes a Percentage adjustment to one FeeCategory's recurring
        // subtotal (catalog 1010) instead of the whole invoice's. Informational only for
        // FixedAmount.
        public string FeeCategoryCode { get; set; }
        public AdjustmentDirection Direction { get; set; }
        public AwardValueType ValueType { get; set; }
        public decimal Value { get; set; }
        public string Remarks { get; set; }
    }
}

using Domain.Enums;

namespace Application.FeeInvoices.Commands
{
    // Pending adjustments only -- Applied ones are immutable (their effect is snapshotted in
    // an invoice). Enrollment and billing period are immutable; re-targeting a different
    // month means cancelling this row and creating a new one.
    public class UpdateFeeAdjustmentCommand
    {
        public string AdjustmentTypeCode { get; set; }
        public string FeeCategoryCode { get; set; }
        public AdjustmentDirection Direction { get; set; }
        public AwardValueType ValueType { get; set; }
        public decimal Value { get; set; }
        public string Remarks { get; set; }
    }
}

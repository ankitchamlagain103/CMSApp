using Domain.Enums;

namespace Application.Employees.Commands
{
    // Pending adjustments only; fiscal year and month are immutable (re-targeting a different
    // month = cancel + recreate), same convention as UpdateFeeAdjustmentCommand.
    public class UpdateSalaryAdjustmentCommand
    {
        public string AdjustmentTypeCode { get; set; }
        public AdjustmentDirection Direction { get; set; }
        public AwardValueType ValueType { get; set; } = AwardValueType.FixedAmount;
        public decimal Value { get; set; }
        public decimal? Quantity { get; set; }
        public string Remarks { get; set; }
    }
}

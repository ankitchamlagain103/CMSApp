using Domain.Enums;

namespace Application.Employees.Commands
{
    // A pre-run monthly payroll override (unpaid leave, late fine, bonus, incentive, ...).
    // For UNPAID_LEAVE, Quantity is the day count and Value is ignored (the deduction is
    // computed from the slip's own monthly earnings at generation time, P4); for every other
    // type, Value is the amount (or percentage of BASIC) and Quantity an optional multiplier
    // (e.g. number of late arrivals).
    public class CreateSalaryAdjustmentCommand
    {
        public Guid FiscalYearId { get; set; }
        public int MonthIndex { get; set; }
        public string AdjustmentTypeCode { get; set; }
        public AdjustmentDirection Direction { get; set; }
        public AwardValueType ValueType { get; set; } = AwardValueType.FixedAmount;
        public decimal Value { get; set; }
        public decimal? Quantity { get; set; }
        public string Remarks { get; set; }
    }
}

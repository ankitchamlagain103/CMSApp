namespace Domain.Constants
{
    // The one SalaryAdjustmentType Config code (TypeCode ConfigTypeCodes.SalaryAdjustmentType)
    // with special generation-time handling: an UNPAID_LEAVE adjustment is entered as a day
    // count (Quantity) and converted to a deduction of Quantity * (monthly recurring earnings /
    // month days) by PayrollRunService, also reducing the slip's PayDays. Every other code is a
    // plain amount/percentage line.
    public static class SalaryAdjustmentTypeCodes
    {
        public const string UnpaidLeave = "UNPAID_LEAVE";
    }
}

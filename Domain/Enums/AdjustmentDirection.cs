namespace Domain.Enums
{
    // Shared by FeeAdjustment ("additional charge" vs "special discount/credit") and
    // SalaryAdjustment ("bonus/incentive/arrear" vs "unpaid leave/late fine"): whether the
    // adjustment increases or decreases what the subject owes/earns. The DTO layer names the
    // sides per domain (Charge/Credit vs Earning/Deduction); the stored value is this shared
    // enum.
    public enum AdjustmentDirection
    {
        Increase = 1,
        Decrease = 2
    }
}

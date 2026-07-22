namespace Domain.Enums
{
    // Shared by FeeAdjustment and SalaryAdjustment: Pending rows are consumed by the next
    // generation of their month (stamped Applied + linked to the produced document, in the
    // run's single SaveChangesAsync); regenerating a Draft document reverts its consumed
    // adjustments to Pending first. Applied rows are immutable; Cancelled is a manual
    // withdrawal while still Pending.
    public enum AdjustmentStatus
    {
        Pending = 1,
        Applied = 2,
        Cancelled = 3
    }
}

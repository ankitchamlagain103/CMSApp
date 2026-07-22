namespace Domain.Enums
{
    // PayrollRun lifecycle: Draft (slips editable) -> Approved (locked, adjustments consumed)
    // -> Paid (disbursement recorded). Cancelled is terminal and frees the (fiscal year, month)
    // slot for regeneration.
    public enum PayrollRunStatus
    {
        Draft = 1,
        Approved = 2,
        Paid = 3,
        Cancelled = 4
    }
}

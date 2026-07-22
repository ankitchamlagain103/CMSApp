namespace Domain.Enums
{
    // FeeInvoice lifecycle. Lines are mutable in Draft only; Generated locks the snapshot.
    // Pending is derived state (Generated + past DueDate + balance remaining), stamped
    // opportunistically rather than by a scheduler -- same self-healing pattern as
    // EmployeeLoan's auto-close. Cancelled is terminal and frees the (enrollment, period)
    // slot for regeneration.
    public enum FeeInvoiceStatus
    {
        Draft = 1,
        Generated = 2,
        Pending = 3,
        PartiallyPaid = 4,
        Paid = 5,
        Cancelled = 6
    }
}

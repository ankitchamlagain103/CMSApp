namespace Domain.Enums
{
    // Payments are append-only money records: a mistake is corrected by voiding (which reverses
    // the payment's allocations and re-derives the affected invoices' statuses) and re-entering,
    // never by editing amounts in place.
    public enum FeePaymentStatus
    {
        Confirmed = 1,
        Voided = 2
    }
}

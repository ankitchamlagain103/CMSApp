namespace Domain.Entities
{
    // How much of one FeePayment settled one FeeInvoice. A payment may settle many invoices
    // (FIFO, oldest billing period first) and an invoice may be settled by many payments.
    // Hard-deleted pure link row (like EnrollmentFeeSelection); rows disappear only when their
    // payment is voided.
    public class FeePaymentAllocation : AuditableEntity
    {
        public Guid Id { get; set; }
        public Guid FeePaymentId { get; set; }
        public Guid FeeInvoiceId { get; set; }
        public decimal Amount { get; set; }

        public virtual FeePayment FeePayment { get; set; }
        public virtual FeeInvoice FeeInvoice { get; set; }
    }
}

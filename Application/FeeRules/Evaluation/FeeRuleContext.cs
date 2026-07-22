namespace Application.FeeRules.Evaluation
{
    // Everything a payment-time rule evaluation needs to know, assembled by the fee-payment
    // preview/confirm flow: which invoices the tendered amount would fully settle (FIFO,
    // oldest billing period first) and when the money arrived. Pure input model -- the engine
    // does no I/O.
    public class FeeRuleContext
    {
        public Guid EnrollmentId { get; set; }
        public Guid AcademicClassId { get; set; }
        public DateTime PaymentDate { get; set; }
        public List<FeeRuleInvoiceContext> FullySettledInvoices { get; set; } = new List<FeeRuleInvoiceContext>();
    }
}

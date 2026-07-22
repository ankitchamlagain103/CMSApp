namespace Application.FeePayments.Queries
{
    // "If I pay for the next N months of this enrollment, how much is that?" -- read-only, never
    // persists anything (2026-07-17). Answers the Collect Fee Payment form's "how do I know the
    // amount for X months" gap: the cashier picks MonthsToPay, this returns the exact amount to
    // put in the Amount field (rule discounts already applied).
    public class GetFeeAdvanceQuoteQuery
    {
        public Guid EnrollmentId { get; set; }
        public int MonthsToPay { get; set; }
        public DateTime? PaymentDate { get; set; }
        public bool ApplyRuleDiscounts { get; set; } = true;
    }
}

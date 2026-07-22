namespace Application.FeeRules.Evaluation
{
    // One fully-settled invoice as seen by the rule engine. RecurringSubtotal is the sum of
    // the invoice's positive recurring lines (StructureItem + AnnualInstallment) -- the base
    // percentage discounts apply to, consistent with the existing "discounts reduce only the
    // recurring total" convention. RecurringSubtotalByCategory supports category-scoped rules.
    public class FeeRuleInvoiceContext
    {
        public Guid FeeInvoiceId { get; set; }
        public int BillingYear { get; set; }
        public int BillingMonth { get; set; }
        public DateTime DueDate { get; set; }
        public decimal RemainingBalance { get; set; }
        public decimal RecurringSubtotal { get; set; }
        public Dictionary<string, decimal> RecurringSubtotalByCategory { get; set; } = new Dictionary<string, decimal>();
    }
}

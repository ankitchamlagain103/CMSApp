namespace Application.FeeRules.Evaluation
{
    // One proposed discount produced by a rule evaluation: a negative RuleDiscount line to be
    // appended to FeeInvoiceId for Amount (stored positive here; the payment flow writes the
    // negative line).
    public class FeeRuleDiscount
    {
        public Guid FeeRuleId { get; set; }
        public string RuleCode { get; set; }
        public string RuleName { get; set; }
        public Guid FeeInvoiceId { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; }
    }
}

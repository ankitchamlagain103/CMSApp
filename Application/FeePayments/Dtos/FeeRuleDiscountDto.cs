namespace Application.FeePayments.Dtos
{
    public class FeeRuleDiscountDto
    {
        public Guid FeeRuleId { get; set; }
        public string RuleCode { get; set; }
        public string RuleName { get; set; }
        public Guid FeeInvoiceId { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; }
    }
}

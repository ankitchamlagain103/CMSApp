namespace Application.FeePayments.Dtos
{
    // The answer to "how much for X months?" -- MonthsAvailable can be less than
    // MonthsRequested if the class has no active fee structure to bill new months against (the
    // already-open months are still quoted). NetAmountToCollect is what to put in the payment
    // form's Amount field.
    public class FeeAdvanceQuoteDto
    {
        public Guid EnrollmentId { get; set; }
        public int MonthsRequested { get; set; }
        public int MonthsAvailable { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal RuleDiscountTotal { get; set; }
        public decimal NetAmountToCollect { get; set; }
        public List<FeeAdvanceQuoteMonthDto> Months { get; set; } = new List<FeeAdvanceQuoteMonthDto>();
        public List<FeeRuleDiscountDto> RuleDiscounts { get; set; } = new List<FeeRuleDiscountDto>();
    }
}

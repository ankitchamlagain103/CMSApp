namespace Application.Enrollments.Dtos
{
    // Totals grouped by billing frequency (an applicable Monthly item and an applicable Annual
    // item can't be meaningfully summed into one number). Discounts/scholarships reduce only
    // MonthlyRecurringTotal -- see fee_management_implementation_guide.md for why. Refundable
    // (deposit-style) items are broken out separately since they aren't a cost.
    public class FeeStructureSummaryDto
    {
        public decimal MonthlyRecurringTotal { get; set; }
        public decimal AnnualTotal { get; set; }
        public decimal OneTimeTotal { get; set; }
        public decimal RefundableDepositTotal { get; set; }
        public decimal TotalDiscountReduction { get; set; }
        public decimal TotalScholarshipReduction { get; set; }
        public decimal NetMonthlyRecurring { get; set; }
    }
}

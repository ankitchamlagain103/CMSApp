namespace Application.Enrollments.Dtos
{
    // Totals grouped by billing frequency (an applicable Monthly item and an applicable Annual
    // item can't be meaningfully summed into one number). Refundable (deposit-style) items are
    // broken out separately since they aren't a cost.
    //
    // MonthlyRecurringTotal (2026-07-17 fix) now reflects the ACTUAL configured billing
    // schedule, not just raw FrequencyType == Monthly items: an Annual item with
    // InstallmentCount >= 2 is genuinely billed monthly (as an AnnualInstallment line on every
    // invoice), so its per-month share (AnnualInstallmentMonthlyShare) is folded into
    // MonthlyRecurringTotal -- this is what previously under-reported "Rs 225/month" for a
    // student whose real monthly bill, once the Rs 100,000 Annual Fee's 12 installments are
    // counted, is closer to Rs 8,500+/month. AnnualTotal still shows the full yearly amount for
    // transparency (installment-split or not). Discounts/scholarships reduce the COMBINED
    // MonthlyRecurringTotal (structure Monthly items + amortized Annual-installment share),
    // matching what a real generated invoice's recurring subtotal (StructureItem +
    // AnnualInstallment lines) actually discounts against.
    public class FeeStructureSummaryDto
    {
        public decimal MonthlyRecurringTotal { get; set; }

        // The portion of MonthlyRecurringTotal that comes from Annual items with
        // InstallmentCount >= 2, broken out so the UI can show "of which Rs X/month is the
        // Annual Fee's installment share" instead of a single opaque number.
        public decimal AnnualInstallmentMonthlyShare { get; set; }
        public decimal AnnualTotal { get; set; }
        public decimal OneTimeTotal { get; set; }
        public decimal RefundableDepositTotal { get; set; }
        public decimal TotalDiscountReduction { get; set; }
        public decimal TotalScholarshipReduction { get; set; }
        public decimal NetMonthlyRecurring { get; set; }
    }
}

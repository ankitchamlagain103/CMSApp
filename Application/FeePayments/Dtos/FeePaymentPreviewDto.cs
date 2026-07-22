namespace Application.FeePayments.Dtos
{
    // The allocation plan + rule evaluation a cashier confirms before money is recorded:
    // "paying these invoices, earning these discounts, leaving this outstanding."
    public class FeePaymentPreviewDto
    {
        public Guid EnrollmentId { get; set; }
        public decimal Amount { get; set; }
        public decimal OutstandingBefore { get; set; }
        public decimal TotalRuleDiscount { get; set; }
        public decimal OutstandingAfter { get; set; }

        // Non-zero when the tendered amount exceeds the outstanding total AFTER rule discounts
        // -- e.g. tendering 3 months' full total and earning an advance-months discount. The
        // preview reports it so the cashier collects exactly (Amount - UnallocatedAmount);
        // confirm rejects while it is non-zero (no credit ledger in this pass, F8).
        public decimal UnallocatedAmount { get; set; }

        // How many not-yet-existing months this payment will bill in advance (2026-07-17,
        // Allocations already lists each one -- look at IsNewlyGenerated per row for which).
        public int MonthsBilledInAdvance { get; set; }
        public List<FeePaymentAllocationDto> Allocations { get; set; } = new List<FeePaymentAllocationDto>();
        public List<FeeRuleDiscountDto> RuleDiscounts { get; set; } = new List<FeeRuleDiscountDto>();
    }
}

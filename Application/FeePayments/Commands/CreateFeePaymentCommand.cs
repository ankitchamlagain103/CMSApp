using Domain.Enums;

namespace Application.FeePayments.Commands
{
    // Used by both preview (no writes) and confirm. ApplyRuleDiscounts lets a cashier decline
    // a proposed rule discount without editing the rule catalog.
    public class CreateFeePaymentCommand
    {
        public Guid EnrollmentId { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public PaymentMode PaymentMode { get; set; }
        public string ReferenceNo { get; set; }
        public string Remarks { get; set; }
        public bool ApplyRuleDiscounts { get; set; } = true;

        // Advance payment (2026-07-17): when the tendered Amount exceeds the enrollment's
        // currently open (already-generated) invoices, the service bills ahead -- creating and
        // immediately finalizing the next N months' invoices (same line composition as regular
        // generation: structure items, opt-in selections, discounts/scholarships, pending
        // adjustments) so the "pay X months together" rule has something to evaluate and the
        // extra money has real invoices to settle against, up to a 12-month cap per payment.
        // Set false to keep the old "amount cannot exceed outstanding" behavior.
        public bool AllowAdvanceBilling { get; set; } = true;
    }
}

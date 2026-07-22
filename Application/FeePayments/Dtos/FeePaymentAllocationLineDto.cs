using Domain.Enums;

namespace Application.FeePayments.Dtos
{
    // One line of an invoice a payment (preview) is settling -- shows the cashier WHAT is being
    // collected (which fee categories, which discounts), not just the invoice's total. Mirrors
    // FeeInvoiceLineDto's shape minus the line id (advance-billed invoices don't exist yet at
    // preview time, so their lines have no ids to expose).
    public class FeePaymentAllocationLineDto
    {
        public FeeLineSource Source { get; set; }
        public string FeeCategoryCode { get; set; }
        public string FeeCategoryLabel { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
    }
}

namespace Application.FeePayments.Dtos
{
    public class FeePaymentAllocationDto
    {
        public Guid FeeInvoiceId { get; set; }
        public string InvoiceNo { get; set; }
        public int BillingYear { get; set; }
        public int BillingMonth { get; set; }
        public decimal Amount { get; set; }
        public bool SettlesInvoice { get; set; }

        // True when this invoice didn't exist before the payment -- it was billed in advance
        // (2026-07-17) to absorb a tendered amount beyond the previously outstanding total.
        public bool IsNewlyGenerated { get; set; }

        // The allocated invoice's line breakdown (2026-07-19) -- filled on the PREVIEW response
        // so the cashier sees which category fees the payment collects before confirming; the
        // confirm/list/detail responses leave it empty (the receipt endpoint already carries the
        // full line detail there).
        public List<FeePaymentAllocationLineDto> Lines { get; set; } = new List<FeePaymentAllocationLineDto>();
    }
}

using Domain.Enums;

namespace Application.FeeInvoices.Dtos
{
    public class FeeInvoiceLineDto
    {
        public Guid Id { get; set; }
        public FeeLineSource Source { get; set; }
        public string FeeCategoryCode { get; set; }

        // Human-readable FeeCategory catalog label (2026-07-19); falls back to the code when
        // the option no longer exists in the catalog.
        public string FeeCategoryLabel { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
    }
}

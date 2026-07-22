namespace Application.FeeInvoices.Commands
{
    // A manual line added to a Draft invoice (Source = Manual). Amount is signed: positive =
    // extra charge, negative = one-off credit.
    public class FeeInvoiceLineInput
    {
        public string FeeCategoryCode { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
    }
}

namespace Application.FeeInvoices.Dtos
{
    // One row of the ledger-style Statement of Account: an invoice posts a Debit (what became
    // owed), a payment posts a Credit (what was received), and Balance is the running
    // debits-minus-credits total after this entry.
    public class FeeAccountEntryDto
    {
        public DateTime Date { get; set; }
        public string EntryType { get; set; }
        public string Reference { get; set; }
        public string Description { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal Balance { get; set; }
    }
}

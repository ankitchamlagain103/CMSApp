namespace Application.FeePayments.Dtos
{
    public class FeeAdvanceQuoteMonthDto
    {
        public int BillingYear { get; set; }
        public int BillingMonth { get; set; }
        public decimal NetAmount { get; set; }

        // False = an invoice already exists for this month (Generated/Pending/PartiallyPaid);
        // true = this month doesn't exist yet and would be billed in advance if the quoted
        // amount is actually paid.
        public bool IsAlreadyGenerated { get; set; }
    }
}

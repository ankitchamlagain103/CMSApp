namespace Application.FeeInvoices.Commands
{
    // Draft-only header edits; everything money-shaped lives on the lines.
    public class UpdateFeeInvoiceCommand
    {
        public DateTime DueDate { get; set; }
        public string Remarks { get; set; }
    }
}

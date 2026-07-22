namespace Application.FeeInvoices.Commands
{
    public class FinalizeFeeInvoicesCommand
    {
        public List<Guid> InvoiceIds { get; set; } = new List<Guid>();
    }
}

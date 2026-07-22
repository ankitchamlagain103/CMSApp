namespace Application.FeeInvoices.Dtos
{
    public class FinalizeResultDto
    {
        public int FinalizedCount { get; set; }
        public List<Guid> SkippedInvoiceIds { get; set; } = new List<Guid>();
    }
}

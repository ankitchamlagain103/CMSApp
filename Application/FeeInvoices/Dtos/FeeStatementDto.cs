namespace Application.FeeInvoices.Dtos
{
    // The parent-facing "what do I owe" answer: every non-cancelled invoice with a running
    // balance, plus the live outstanding total (F7 -- computed across open invoices, never
    // from the per-invoice PreviousDueAmount snapshots).
    public class FeeStatementDto
    {
        public Guid EnrollmentId { get; set; }
        public string StudentName { get; set; }
        public string AdmissionNo { get; set; }
        public string GradeCode { get; set; }
        public string SectionCode { get; set; }
        public decimal OutstandingAmount { get; set; }
        public List<FeeInvoiceDto> Invoices { get; set; } = new List<FeeInvoiceDto>();
    }
}

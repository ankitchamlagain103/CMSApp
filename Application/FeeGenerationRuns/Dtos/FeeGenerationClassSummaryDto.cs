namespace Application.FeeGenerationRuns.Dtos
{
    public class FeeGenerationClassSummaryDto
    {
        public Guid AcademicClassId { get; set; }

        // Config code, resolved to GradeLabel server-side (2026-08-05).
        public string GradeCode { get; set; }
        public string GradeLabel { get; set; }
        public int InvoiceCount { get; set; }
        public int StudentCount { get; set; }

        // Still-editable invoices in this class -- drives the "Finalize Drafts (N)" action on
        // the class row without the caller needing the full student/invoice breakdown first.
        public int DraftInvoiceCount { get; set; }
        public decimal TotalNetAmount { get; set; }
        public decimal TotalPaidAmount { get; set; }
        public decimal TotalOutstandingAmount { get; set; }
    }
}

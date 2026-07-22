namespace Application.FeeGenerationRuns.Dtos
{
    public class FeeGenerationClassSummaryDto
    {
        public Guid AcademicClassId { get; set; }

        // Config code -- the UI resolves the Nursery/LKG/... display label from the Grade
        // dropdown, same convention as AcademicClassDto.GradeCode.
        public string GradeCode { get; set; }
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

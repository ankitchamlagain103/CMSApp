namespace Application.FeeGenerationRuns.Dtos
{
    public class FeeGenerationClassGroupDto
    {
        public Guid AcademicClassId { get; set; }

        // Config code, resolved to GradeLabel server-side (2026-08-05).
        public string GradeCode { get; set; }
        public string GradeLabel { get; set; }
        public int InvoiceCount { get; set; }
        public int StudentCount { get; set; }

        // Still-editable invoices in this class -- drives the class-detail page's
        // "Finalize Drafts (N)" action.
        public int DraftInvoiceCount { get; set; }
        public decimal TotalNetAmount { get; set; }
        public decimal TotalPaidAmount { get; set; }
        public decimal TotalOutstandingAmount { get; set; }
        public List<FeeGenerationStudentGroupDto> Students { get; set; } = new List<FeeGenerationStudentGroupDto>();
    }
}

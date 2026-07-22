namespace Application.FeeGenerationRuns.Dtos
{
    public class FeeGenerationRunDto
    {
        public Guid Id { get; set; }
        public Guid AcademicYearId { get; set; }
        public string AcademicYearCode { get; set; }
        public int BillingYear { get; set; }
        public int BillingMonth { get; set; }
        public DateTime GeneratedTs { get; set; }
        public DateTime? LastRegeneratedTs { get; set; }
        public string Remarks { get; set; }

        // Who first ran generate for this period (CreatedBy) and when (CreatedTs, same instant
        // as GeneratedTs) -- "who generated this run."
        public string CreatedBy { get; set; }
        public DateTimeOffset CreatedTs { get; set; }

        // Rollup stats computed live from this period's current FeeInvoices -- never stored, so
        // they always reflect the latest generation/payment state regardless of which scoped
        // generate call(s) contributed to the period.
        public int InvoiceCount { get; set; }
        public int ClassCount { get; set; }
        public int StudentCount { get; set; }
        public decimal TotalNetAmount { get; set; }
        public decimal TotalPaidAmount { get; set; }
        public decimal TotalOutstandingAmount { get; set; }
    }
}

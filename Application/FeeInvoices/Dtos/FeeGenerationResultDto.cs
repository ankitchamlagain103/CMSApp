namespace Application.FeeInvoices.Dtos
{
    // What a generation run did. 2026-07-21: skipped[] used to be one row per skipped
    // enrollment, which balloons to hundreds of near-identical rows the moment a fully-generated
    // month is regenerated -- simplified to counts grouped by reason instead.
    public class FeeGenerationResultDto
    {
        // The period's FeeGenerationRun -- deep-links straight to its class/student/invoice
        // breakdown.
        public Guid FeeGenerationRunId { get; set; }
        public int BillingYear { get; set; }
        public int BillingMonth { get; set; }
        public int GeneratedCount { get; set; }
        public int SkippedCount { get; set; }
        public List<Guid> GeneratedInvoiceIds { get; set; } = new List<Guid>();
        public List<FeeGenerationSkipSummaryDto> SkippedSummary { get; set; } = new List<FeeGenerationSkipSummaryDto>();
    }
}
